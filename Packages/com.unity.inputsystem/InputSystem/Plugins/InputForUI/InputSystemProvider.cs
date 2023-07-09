#if UNITY_2023_2_OR_NEWER // UnityEngine.InputForUI Module unavailable in earlier releases
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
        InputEventPartialProvider m_InputEventPartialProvider;

        Configuration m_Cfg;

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

        static InputSystemProvider()
        {
            // Only if InputSystem is enabled in the PlayerSettings do we set it as the provider.
            // This includes situations where both InputManager and InputSystem are enabled.
#if ENABLE_INPUT_SYSTEM
            EventProvider.SetInputSystemProvider(new InputSystemProvider());
#endif
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

            // TODO should UITK somehow override this?
            m_Cfg = Configuration.GetDefaultConfiguration();
            RegisterActions(m_Cfg);
        }

        public void Shutdown()
        {
            UnregisterActions(m_Cfg);

            m_InputEventPartialProvider.Shutdown();
            m_InputEventPartialProvider = null;
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
                    return m_MoveAction.action.activeControl.device;
                case NavigationEvent.Direction.Next:
                case NavigationEvent.Direction.Previous:
                    return m_NextPreviousAction.activeControl.device;
                case NavigationEvent.Direction.None:
                default:
                    return Keyboard.current;
            }
        }

        (Vector2, bool) ReadCurrentNavigationMoveVector()
        {
            var move = m_MoveAction.action.ReadValue<Vector2>();
            // Check if the action was "pressed" this frame to deal with repeating events
            var axisWasPressed = m_MoveAction.action.WasPressedThisFrame();
            return (move, axisWasPressed);
        }

        NavigationEvent.Direction ReadNextPreviousDirection()
        {
            if (m_NextPreviousAction.IsPressed())
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
            var scrollDelta = ctx.ReadValue<Vector2>();
            if (scrollDelta.sqrMagnitude < k_SmallestReportedMovementSqrDist)
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

            // Make it look similar to IMGUI event scroll values.
            scrollDelta.x *= kScrollUGUIScaleFactor;
            scrollDelta.y *= -kScrollUGUIScaleFactor;

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

        void UnregisterNextPreviousAction()
        {
            if (m_NextPreviousAction != null)
            {
                m_NextPreviousAction.Disable();
                m_NextPreviousAction = null;
            }
        }

        void RegisterActions(Configuration cfg)
        {
            m_InputActionAsset = InputActionAsset.FromJson(cfg.InputActionAssetAsJson);

            m_PointAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.PointAction));
            m_MoveAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.MoveAction));
            m_SubmitAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.SubmitAction));
            m_CancelAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.CancelAction));
            m_LeftClickAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.LeftClickAction));
            m_MiddleClickAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.MiddleClickAction));
            m_RightClickAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.RightClickAction));
            m_ScrollWheelAction = InputActionReference.Create(m_InputActionAsset.FindAction(m_Cfg.ScrollWheelAction));

            if (m_PointAction.action != null)
                m_PointAction.action.performed += OnPointerPerformed;

            if (m_SubmitAction.action != null)
                m_SubmitAction.action.performed += OnSubmitPerformed;

            if (m_CancelAction.action != null)
                m_CancelAction.action.performed += OnCancelPerformed;

            if (m_LeftClickAction.action != null)
                m_LeftClickAction.action.performed += OnLeftClickPerformed;

            if (m_MiddleClickAction.action != null)
                m_MiddleClickAction.action.performed += OnMiddleClickPerformed;

            if (m_RightClickAction.action != null)
                m_RightClickAction.action.performed += OnRightClickPerformed;

            if (m_ScrollWheelAction.action != null)
                m_ScrollWheelAction.action.performed += OnScrollWheelPerformed;

            // When adding new one's don't forget to add them to UnregisterActions

            m_InputActionAsset.Enable();

            // TODO make it configurable as it is not part of default config
            // The Next/Previous action is not part of the input actions asset
            RegisterNextPreviousAction();
        }

        void UnregisterActions(Configuration cfg)
        {
            if (m_PointAction.action != null)
                m_PointAction.action.performed -= OnPointerPerformed;

            if (m_SubmitAction.action != null)
                m_SubmitAction.action.performed -= OnSubmitPerformed;

            if (m_CancelAction.action != null)
                m_CancelAction.action.performed -= OnCancelPerformed;

            if (m_LeftClickAction.action != null)
                m_LeftClickAction.action.performed -= OnLeftClickPerformed;

            if (m_MiddleClickAction.action != null)
                m_MiddleClickAction.action.performed -= OnMiddleClickPerformed;

            if (m_RightClickAction.action != null)
                m_RightClickAction.action.performed -= OnRightClickPerformed;

            if (m_ScrollWheelAction.action != null)
                m_ScrollWheelAction.action.performed -= OnScrollWheelPerformed;

            m_PointAction = null;
            m_MoveAction = null;
            m_SubmitAction = null;
            m_CancelAction = null;
            m_LeftClickAction = null;
            m_MiddleClickAction = null;
            m_RightClickAction = null;
            m_ScrollWheelAction = null;

            m_InputActionAsset.Disable();

            // The Next/Previous action is not part of the input actions asset
            UnregisterNextPreviousAction();

            UnityEngine.Object.Destroy(m_InputActionAsset); // TODO check if this is ok
        }

        public struct Configuration
        {
            public string InputActionAssetAsJson;
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
                // TODO this is a weird way of doing that, is there an easier way?
                var asset = new DefaultInputActions();
                var json = asset.asset.ToJson();
                UnityEngine.Object.DestroyImmediate(asset.asset); // TODO just Dispose doesn't work in edit mode

                return new Configuration
                {
                    InputActionAssetAsJson = json,
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
    }
}
#endif // UNITY_2023_2_OR_NEWER
