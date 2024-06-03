#if UNITY_2023_2_OR_NEWER // UnityEngine.InputForUI Module unavailable in earlier releases
using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputForUI;

namespace UnityEngine.InputSystem.Plugins.InputForUI
{
    using Event = UnityEngine.InputForUI.Event;
    using EventModifiers = UnityEngine.InputForUI.EventModifiers;
    using EventProvider = UnityEngine.InputForUI.EventProvider;

    internal class InputSystemProvider : IEventProviderImpl
    {
        Configuration m_Cfg;

        InputEventPartialProvider m_InputEventPartialProvider;

        InputActionAsset m_InputActionAsset;

        InputActionReference m_PointAction;
        InputActionReference m_MoveAction;
        InputActionReference m_SubmitAction;
        InputActionReference m_CancelAction;
        InputActionReference m_LeftClickAction;
        InputActionReference m_MiddleClickAction;
        InputActionReference m_RightClickAction;
        InputActionReference m_ScrollWheelAction;

        InputAction m_NextPreviousAction;

        List<Event> m_Events = new List<Event>();

        PointerState m_MouseState;

        PointerState m_PenState;
        bool m_SeenPenEvents;

        PointerState m_TouchState;
        bool m_SeenTouchEvents;

        const float k_SmallestReportedMovementSqrDist = 0.01f;

        NavigationEventRepeatHelper m_RepeatHelper = new();
        bool m_ResetSeenEventsOnUpdate;

        const float kScrollUGUIScaleFactor = 3.0f;

        static Action<InputActionAsset> s_OnRegisterActions;

        static InputSystemProvider()
        {
            // Only if InputSystem is enabled in the PlayerSettings do we set it as the provider.
            // This includes situations where both InputManager and InputSystem are enabled.
#if ENABLE_INPUT_SYSTEM
            EventProvider.SetInputSystemProvider(new InputSystemProvider());
#endif // ENABLE_INPUT_SYSTEM
        }

        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Bootstrap() {} // Empty function. Exists only to invoke the static class constructor in Runtime Players

        EventModifiers m_EventModifiers => m_InputEventPartialProvider._eventModifiers;

        DiscreteTime m_CurrentTime => (DiscreteTime)Time.timeAsRational;

        const uint k_DefaultPlayerId = 0u;

        public void Initialize()
        {
            m_InputEventPartialProvider ??= new InputEventPartialProvider();
            m_InputEventPartialProvider.Initialize();

            m_Events.Clear();

            m_MouseState.Reset();

            m_PenState.Reset();
            m_SeenPenEvents = false;

            m_TouchState.Reset();
            m_SeenTouchEvents = false;

            m_Cfg = Configuration.GetDefaultConfiguration();

            RegisterActions();

            InputSystem.onActionsChange += OnActionsChange;
        }

        public void Shutdown()
        {
            UnregisterActions();

            m_InputEventPartialProvider.Shutdown();
            m_InputEventPartialProvider = null;

            InputSystem.onActionsChange -= OnActionsChange;
        }

        public void OnActionsChange()
        {
            UnregisterActions();

            m_Cfg = Configuration.GetDefaultConfiguration();
            RegisterActions();
        }

        public void Update()
        {
#if UNITY_EDITOR
            // Ensure we are in a good (initialized) state before running updates.
            // This could be in a bad state for a duration while the build pipeline is running
            // when building tests to run in the Standalone Player.
            if (m_InputActionAsset == null)
                return;
#endif

            m_InputEventPartialProvider.Update();

            // Sort events added by input actions callbacks, based on type.
            // This is necessary to ensure that events are dispatched in the correct order.
            // If all events are of the PointerEvents type, sorting is based on reverse order of the EventSource enum.
            // Touch -> Pen -> Mouse.
            m_Events.Sort(SortEvents);

            var currentTime = (DiscreteTime)Time.timeAsRational;

            DirectionNavigation(currentTime);

            foreach (var ev in m_Events)
            {
                // We need to ignore some pointer events based on priority (Touch->Pen->Mouse)
                // This is mostly used to filter out simulated input, e.g. when pen is active it also generates mouse input
                if (m_SeenTouchEvents && ev.type == Event.Type.PointerEvent && ev.eventSource == EventSource.Pen)
                    m_PenState.Reset();
                else if ((m_SeenTouchEvents || m_SeenPenEvents) &&
                         ev.type == Event.Type.PointerEvent && (ev.eventSource == EventSource.Mouse || ev.eventSource == EventSource.Unspecified))
                    m_MouseState.Reset();
                else
                    EventProvider.Dispatch(ev);
            }

            // Sometimes single lower priority events can be received when using Touch or Pen, on a different frame.
            // To avoid dispatching them, the seen event flags aren't reset in between calls to OnPointerPerformed.
            // Essentially, if we're moving with Touch or Pen, lower priority events aren't dispatch as well.
            // Once OnClickPerformed is called, the seen flags are reset
            if (m_ResetSeenEventsOnUpdate)
            {
                ResetSeenEvents();
                m_ResetSeenEventsOnUpdate = false;
            }

            m_Events.Clear();
        }

        void ResetSeenEvents()
        {
            m_SeenTouchEvents = false;
            m_SeenPenEvents = false;
        }

        public bool ActionAssetIsNotNull()
        {
            return m_InputActionAsset != null;
        }

        //TODO: Refactor as there is no need for having almost the same implementation in the IM and ISX?
        void DirectionNavigation(DiscreteTime currentTime)
        {
            var(move, axesButtonWerePressed) = ReadCurrentNavigationMoveVector();
            var direction = NavigationEvent.DetermineMoveDirection(move);

            // Checks for next/previous directions if no movement was detected
            if (direction == NavigationEvent.Direction.None)
            {
                direction = ReadNextPreviousDirection();
                axesButtonWerePressed = m_NextPreviousAction.WasPressedThisFrame();
            }

            if (direction == NavigationEvent.Direction.None)
            {
                m_RepeatHelper.Reset();
            }
            else
            {
                if (m_RepeatHelper.ShouldSendMoveEvent(currentTime, direction, axesButtonWerePressed))
                {
                    EventProvider.Dispatch(Event.From(new NavigationEvent
                    {
                        type = NavigationEvent.Type.Move,
                        direction = direction,
                        timestamp = currentTime,
                        eventSource = GetEventSource(GetActiveDeviceFromDirection(direction)),
                        playerId = k_DefaultPlayerId,
                        eventModifiers = m_EventModifiers
                    }));
                }
            }
        }

        InputDevice GetActiveDeviceFromDirection(NavigationEvent.Direction direction)
        {
            switch (direction)
            {
                case NavigationEvent.Direction.Left:
                case NavigationEvent.Direction.Up:
                case NavigationEvent.Direction.Right:
                case NavigationEvent.Direction.Down:
                    if (m_MoveAction != null)
                        return m_MoveAction.action.activeControl.device;
                    break;
                case NavigationEvent.Direction.Next:
                case NavigationEvent.Direction.Previous:
                    if (m_NextPreviousAction != null)
                        return m_NextPreviousAction.activeControl.device;
                    break;
                case NavigationEvent.Direction.None:
                default:
                    break;
            }

            return Keyboard.current;
        }

        (Vector2, bool) ReadCurrentNavigationMoveVector()
        {
            // In case action has not been configured we return defaults
            if (m_MoveAction == null)
                return (default, default);

            var move = m_MoveAction.action.ReadValue<Vector2>();
            // Check if the action was "pressed" this frame to deal with repeating events
            var axisWasPressed = m_MoveAction.action.WasPressedThisFrame();
            return (move, axisWasPressed);
        }

        NavigationEvent.Direction ReadNextPreviousDirection()
        {
            if (m_NextPreviousAction.IsPressed()) // Note: never null since created through code
            {
                //TODO: For now it only deals with Keyboard, needs to deal with other devices if we can add bindings
                //      for Gamepad, etc
                //TODO: An alternative could be to have an action for next and for previous since shortcut support does
                //      not work properly
                if (m_NextPreviousAction.activeControl.device is Keyboard)
                {
                    var keyboard = m_NextPreviousAction.activeControl.device as Keyboard;
                    // Return direction based on whether shift is pressed or not
                    return keyboard.shiftKey.isPressed ? NavigationEvent.Direction.Previous : NavigationEvent.Direction.Next;
                }
            }

            return NavigationEvent.Direction.None;
        }

        static int SortEvents(Event a, Event b)
        {
            return Event.CompareType(a, b);
        }

        public void OnFocusChanged(bool focus)
        {
            m_InputEventPartialProvider.OnFocusChanged(focus);
        }

        public bool RequestCurrentState(Event.Type type)
        {
            if (m_InputEventPartialProvider.RequestCurrentState(type))
                return true;

            switch (type)
            {
                case Event.Type.PointerEvent:
                {
                    if (m_TouchState.LastPositionValid)
                        EventProvider.Dispatch(Event.From(ToPointerStateEvent(m_CurrentTime, m_TouchState, EventSource.Touch)));
                    if (m_PenState.LastPositionValid)
                        EventProvider.Dispatch(Event.From(ToPointerStateEvent(m_CurrentTime, m_PenState, EventSource.Pen)));
                    if (m_MouseState.LastPositionValid)
                        EventProvider.Dispatch(Event.From(ToPointerStateEvent(m_CurrentTime, m_MouseState, EventSource.Mouse)));
                    else
                    {
                        // TODO maybe it's reasonable to poll and dispatch mouse state here anyway?
                    }

                    return m_TouchState.LastPositionValid ||
                        m_PenState.LastPositionValid ||
                        m_MouseState.LastPositionValid;
                }
                // TODO
                case Event.Type.IMECompositionEvent:
                default:
                    return false;
            }
        }

        public uint playerCount => 1; // TODO

        // copied from UIElementsRuntimeUtility.cs
        static Vector2 ScreenBottomLeftToPanelPosition(Vector2 position, int targetDisplay)
        {
            // Flip positions Y axis between input and UITK
            var screenHeight = Screen.height;
            if (targetDisplay > 0 && targetDisplay < Display.displays.Length)
                screenHeight = Display.displays[targetDisplay].systemHeight;
            position.y = screenHeight - position.y;
            return position;
        }

        PointerEvent ToPointerStateEvent(DiscreteTime currentTime, in PointerState state, EventSource eventSource)
        {
            return new PointerEvent
            {
                type = PointerEvent.Type.State,
                pointerIndex = 0,
                position = state.LastPosition,
                deltaPosition = Vector2.zero,
                scroll = Vector2.zero,
                displayIndex = state.LastDisplayIndex,
                // TODO
                // tilt = eventSource == EventSource.Pen ? _lastPenData.tilt : Vector2.zero,
                // twist = eventSource == EventSource.Pen ? _lastPenData.twist : 0.0f,
                // pressure = eventSource == EventSource.Pen ? _lastPenData.pressure : 0.0f,
                // isInverted = eventSource == EventSource.Pen && ((_lastPenData.penStatus & PenStatus.Inverted) != 0),
                button = 0,
                buttonsState = state.ButtonsState,
                clickCount = 0,
                timestamp = currentTime,
                eventSource = eventSource,
                playerId = k_DefaultPlayerId,
                eventModifiers = m_EventModifiers
            };
        }

        EventSource GetEventSource(InputAction.CallbackContext ctx)
        {
            var device = ctx.control.device;
            return GetEventSource(device);
        }

        EventSource GetEventSource(InputDevice device)
        {
            if (device is Touchscreen)
                return EventSource.Touch;
            if (device is Pen)
                return EventSource.Pen;
            if (device is Mouse)
                return EventSource.Mouse;
            if (device is Keyboard)
                return EventSource.Keyboard;
            if (device is Gamepad)
                return EventSource.Gamepad;

            return EventSource.Unspecified;
        }

        ref PointerState GetPointerStateForSource(EventSource eventSource)
        {
            switch (eventSource)
            {
                case EventSource.Touch:
                    return ref m_TouchState;
                case EventSource.Pen:
                    return ref m_PenState;
                default:
                    return ref m_MouseState;
            }
        }

        void DispatchFromCallback(in Event ev)
        {
            m_Events.Add(ev);
        }

        static int FindTouchFingerIndex(Touchscreen touchscreen, InputAction.CallbackContext ctx)
        {
            if (touchscreen == null)
                return 0;

            var asVector2Control = ctx.control is Vector2Control ? (Vector2Control)ctx.control : null;
            var asTouchPressControl = ctx.control is TouchPressControl ? (TouchPressControl)ctx.control : null;
            var asTouchControl = ctx.control is TouchControl ? (TouchControl)ctx.control : null;

            // Finds the index of the matching control type in the Touchscreen device lost of touch controls (touches)
            for (var i = 0; i < touchscreen.touches.Count; ++i)
            {
                if (asVector2Control != null && asVector2Control == touchscreen.touches[i].position)
                    return i;
                if (asTouchPressControl != null && asTouchPressControl == touchscreen.touches[i].press)
                    return i;
                if (asTouchControl != null && asTouchControl == touchscreen.touches[i])
                    return i;
            }
            return 0;
        }

        void OnPointerPerformed(InputAction.CallbackContext ctx)
        {
            var eventSource = GetEventSource(ctx);
            ref var pointerState = ref GetPointerStateForSource(eventSource);

            // Overall I'm not happy how leaky this is, we're using input actions to have flexibility to bind to different controls,
            // but instead we just kinda abuse it to bind to different devices ...
            var asPointerDevice = ctx.control.device is Pointer ? (Pointer)ctx.control.device : null;
            var asPenDevice = ctx.control.device is Pen ? (Pen)ctx.control.device : null;
            var asTouchscreenDevice = ctx.control.device is Touchscreen ? (Touchscreen)ctx.control.device : null;
            var asTouchControl = ctx.control is TouchControl ? (TouchControl)ctx.control : null;
            var pointerIndex = FindTouchFingerIndex(asTouchscreenDevice, ctx);

            m_ResetSeenEventsOnUpdate = false;
            if (asTouchControl != null || asTouchscreenDevice != null)
                m_SeenTouchEvents = true;
            else if (asPenDevice != null)
                m_SeenPenEvents = true;

            var positionISX = ctx.ReadValue<Vector2>();
            var targetDisplay = asPointerDevice != null ? asPointerDevice.displayIndex.ReadValue() : (asTouchscreenDevice != null ? asTouchscreenDevice.displayIndex.ReadValue() : 0);
            var position = ScreenBottomLeftToPanelPosition(positionISX, targetDisplay);
            var delta = pointerState.LastPositionValid ? position - pointerState.LastPosition : Vector2.zero;

            var tilt = asPenDevice != null ? asPenDevice.tilt.ReadValue() : Vector2.zero;
            var twist = asPenDevice != null ? asPenDevice.twist.ReadValue() : 0.0f;
            var pressure = asPenDevice != null
                ? asPenDevice.pressure.ReadValue()
                : (asTouchControl != null ? asTouchControl.pressure.ReadValue() : 0.0f);
            var isInverted = asPenDevice != null
                ? asPenDevice.eraser.isPressed
                : false; // TODO any way to detect that pen is inverted but not touching?

            if (delta.sqrMagnitude >= k_SmallestReportedMovementSqrDist)
            {
                DispatchFromCallback(Event.From(new PointerEvent
                {
                    type = PointerEvent.Type.PointerMoved,
                    pointerIndex = pointerIndex,
                    position = position,
                    deltaPosition = delta,
                    scroll = Vector2.zero,
                    displayIndex = targetDisplay,
                    tilt = tilt,
                    twist = twist,
                    pressure = pressure,
                    isInverted = isInverted,
                    button = 0,
                    buttonsState = pointerState.ButtonsState,
                    clickCount = 0,
                    timestamp = m_CurrentTime,
                    eventSource = eventSource,
                    playerId = k_DefaultPlayerId,
                    eventModifiers = m_EventModifiers
                }));

                // only record if we send an event
                pointerState.OnMove(m_CurrentTime, position, targetDisplay);
            }
            else if (!pointerState.LastPositionValid)
                pointerState.OnMove(m_CurrentTime, position, targetDisplay);
        }

        void OnSubmitPerformed(InputAction.CallbackContext ctx)
        {
            DispatchFromCallback(Event.From(new NavigationEvent
            {
                type = NavigationEvent.Type.Submit,
                direction = NavigationEvent.Direction.None,
                timestamp = m_CurrentTime,
                eventSource = GetEventSource(ctx),
                playerId = k_DefaultPlayerId,
                eventModifiers = m_EventModifiers
            }));
        }

        void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            DispatchFromCallback(Event.From(new NavigationEvent
            {
                type = NavigationEvent.Type.Cancel,
                direction = NavigationEvent.Direction.None,
                timestamp = m_CurrentTime,
                eventSource = GetEventSource(ctx),
                playerId = k_DefaultPlayerId,
                eventModifiers = m_EventModifiers
            }));
        }

        void OnClickPerformed(InputAction.CallbackContext ctx, EventSource eventSource, PointerEvent.Button button)
        {
            ref var state = ref GetPointerStateForSource(eventSource);

            var asTouchscreenDevice = ctx.control.device is Touchscreen ? (Touchscreen)ctx.control.device : null;
            var asTouchControl = ctx.control is TouchControl ? (TouchControl)ctx.control : null;
            var pointerIndex = FindTouchFingerIndex(asTouchscreenDevice, ctx);

            m_ResetSeenEventsOnUpdate = true;
            if (asTouchControl != null || asTouchscreenDevice != null)
                m_SeenTouchEvents = true;

            var wasPressed = state.ButtonsState.Get(button);
            var isPressed = ctx.ReadValueAsButton();
            state.OnButtonChange(m_CurrentTime, button, wasPressed, isPressed);

            DispatchFromCallback(Event.From(new PointerEvent
            {
                type = isPressed ? PointerEvent.Type.ButtonPressed : PointerEvent.Type.ButtonReleased,
                pointerIndex = pointerIndex,
                position = state.LastPosition,
                deltaPosition = Vector2.zero,
                scroll = Vector2.zero,
                displayIndex = state.LastDisplayIndex,
                tilt = Vector2.zero,
                twist = 0.0f,
                pressure = 0.0f,
                isInverted = false,
                button = button,
                buttonsState = state.ButtonsState,
                clickCount = state.ClickCount,
                timestamp = m_CurrentTime,
                eventSource = eventSource,
                playerId = k_DefaultPlayerId,
                eventModifiers = m_EventModifiers
            }));
        }

        void OnLeftClickPerformed(InputAction.CallbackContext ctx) => OnClickPerformed(ctx, GetEventSource(ctx), PointerEvent.Button.MouseLeft);
        void OnMiddleClickPerformed(InputAction.CallbackContext ctx) => OnClickPerformed(ctx, GetEventSource(ctx), PointerEvent.Button.MouseMiddle);
        void OnRightClickPerformed(InputAction.CallbackContext ctx) => OnClickPerformed(ctx, GetEventSource(ctx), PointerEvent.Button.MouseRight);

        void OnScrollWheelPerformed(InputAction.CallbackContext ctx)
        {
            // ISXB-704: convert input value to uniform ticks before sending them to UI.
            var scrollTicks = ctx.ReadValue<Vector2>() / InputSystem.scrollWheelDeltaPerTick;
            if (scrollTicks.sqrMagnitude < k_SmallestReportedMovementSqrDist)
                return;

            var eventSource = GetEventSource(ctx);
            ref var state = ref GetPointerStateForSource(eventSource);

            var position = Vector2.zero;
            var targetDisplay = 0;

            if (state.LastPositionValid)
            {
                position = state.LastPosition;
                targetDisplay = state.LastDisplayIndex;
            }
            else if (eventSource == EventSource.Mouse && Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                targetDisplay = Mouse.current.displayIndex.ReadValue();
            }

            // Make scrollDelta look similar to IMGUI event scroll values.
            var scrollDelta = new Vector2
            {
                x = scrollTicks.x * kScrollUGUIScaleFactor,
                y = -scrollTicks.y * kScrollUGUIScaleFactor
            };

            DispatchFromCallback(Event.From(new PointerEvent
            {
                type = PointerEvent.Type.Scroll,
                pointerIndex = 0,
                position = position,
                deltaPosition = Vector2.zero,
                scroll = scrollDelta,
                displayIndex = targetDisplay,
                tilt = Vector2.zero,
                twist = 0.0f,
                pressure = 0.0f,
                isInverted = false,
                button = 0,
                buttonsState = state.ButtonsState,
                clickCount = 0,
                timestamp = m_CurrentTime,
                eventSource = EventSource.Mouse,
                playerId = k_DefaultPlayerId,
                eventModifiers = m_EventModifiers
            }));
        }

        void RegisterNextPreviousAction()
        {
            m_NextPreviousAction = new InputAction(name: "nextPreviousAction", type: InputActionType.Button);
            // TODO add more default bindings, or make them configurable
            m_NextPreviousAction.AddBinding("<Keyboard>/tab");
            m_NextPreviousAction.Enable();
        }

        void UnregisterFixedActions()
        {
            // The Next/Previous action is not part of the input actions asset
            if (m_NextPreviousAction != null)
            {
                m_NextPreviousAction.Disable();
                m_NextPreviousAction = null;
            }
        }

        void RegisterActions()
        {
            m_InputActionAsset = m_Cfg.ActionAsset;

            // Invoke potential lister observing registration
            s_OnRegisterActions?.Invoke(m_InputActionAsset);

            m_PointAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.PointAction));
            m_MoveAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.MoveAction));
            m_SubmitAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.SubmitAction));
            m_CancelAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.CancelAction));
            m_LeftClickAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.LeftClickAction));
            m_MiddleClickAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.MiddleClickAction));
            m_RightClickAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.RightClickAction));
            m_ScrollWheelAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.ScrollWheelAction));

            if (m_PointAction != null && m_PointAction.action != null)
                m_PointAction.action.performed += OnPointerPerformed;

            if (m_SubmitAction != null && m_SubmitAction.action != null)
                m_SubmitAction.action.performed += OnSubmitPerformed;

            if (m_CancelAction != null && m_CancelAction.action != null)
                m_CancelAction.action.performed += OnCancelPerformed;

            if (m_LeftClickAction != null && m_LeftClickAction.action != null)
                m_LeftClickAction.action.performed += OnLeftClickPerformed;

            if (m_MiddleClickAction != null && m_MiddleClickAction.action != null)
                m_MiddleClickAction.action.performed += OnMiddleClickPerformed;

            if (m_RightClickAction != null && m_RightClickAction.action != null)
                m_RightClickAction.action.performed += OnRightClickPerformed;

            if (m_ScrollWheelAction != null && m_ScrollWheelAction.action != null)
                m_ScrollWheelAction.action.performed += OnScrollWheelPerformed;

            // When adding new actions, don't forget to add them to UnregisterActions

            if (InputSystem.actions == null)
            {
                // If we've not loaded a user-created set of actions, just enable the UI actions from our defaults.
                m_InputActionAsset.FindActionMap("UI", true).Enable();
            }
            else
                m_InputActionAsset.Enable();

            // TODO make it configurable as it is not part of default config
            // The Next/Previous action is not part of the input actions asset
            RegisterNextPreviousAction();
        }

        void UnregisterActions()
        {
            if (m_PointAction != null && m_PointAction.action != null)
                m_PointAction.action.performed -= OnPointerPerformed;

            if (m_SubmitAction != null && m_SubmitAction.action != null)
                m_SubmitAction.action.performed -= OnSubmitPerformed;

            if (m_CancelAction != null && m_CancelAction.action != null)
                m_CancelAction.action.performed -= OnCancelPerformed;

            if (m_LeftClickAction != null && m_LeftClickAction.action != null)
                m_LeftClickAction.action.performed -= OnLeftClickPerformed;

            if (m_MiddleClickAction != null && m_MiddleClickAction.action != null)
                m_MiddleClickAction.action.performed -= OnMiddleClickPerformed;

            if (m_RightClickAction != null && m_RightClickAction.action != null)
                m_RightClickAction.action.performed -= OnRightClickPerformed;

            if (m_ScrollWheelAction != null && m_ScrollWheelAction.action != null)
                m_ScrollWheelAction.action.performed -= OnScrollWheelPerformed;

            m_PointAction = null;
            m_MoveAction = null;
            m_SubmitAction = null;
            m_CancelAction = null;
            m_LeftClickAction = null;
            m_MiddleClickAction = null;
            m_RightClickAction = null;
            m_ScrollWheelAction = null;

            if (m_InputActionAsset != null)
                m_InputActionAsset.Disable();

            UnregisterFixedActions();
        }

        public struct Configuration
        {
            public InputActionAsset ActionAsset;
            public string PointAction;
            public string MoveAction;
            public string SubmitAction;
            public string CancelAction;
            public string LeftClickAction;
            public string MiddleClickAction;
            public string RightClickAction;
            public string ScrollWheelAction;

            public static Configuration GetDefaultConfiguration()
            {
                // Only use default actions asset configuration if (ISX-1954):
                // - Project-wide Input Actions have not been configured, OR
                // - Project-wide Input Actions have been configured but contains no UI action map.
                var projectWideInputActions = InputSystem.actions;
                var useProjectWideInputActions =
                    projectWideInputActions != null &&
                    projectWideInputActions.FindActionMap("UI") != null;

                // Use InputSystem.actions (Project-wide Actions) if available, else use default asset if
                // user didn't specifically set one, so that UI functions still work (ISXB-811).
                return new Configuration
                {
                    ActionAsset = useProjectWideInputActions ? InputSystem.actions : new DefaultInputActions().asset,
                    PointAction = "UI/Point",
                    MoveAction = "UI/Navigate",
                    SubmitAction = "UI/Submit",
                    CancelAction = "UI/Cancel",
                    LeftClickAction = "UI/Click",
                    MiddleClickAction = "UI/MiddleClick",
                    RightClickAction = "UI/RightClick",
                    ScrollWheelAction = "UI/ScrollWheel",
                };
            }
        }

        internal static void SetOnRegisterActions(Action<InputActionAsset> callback)
        {
            s_OnRegisterActions = callback;
        }
    }
}
#endif // UNITY_2023_2_OR_NEWER
