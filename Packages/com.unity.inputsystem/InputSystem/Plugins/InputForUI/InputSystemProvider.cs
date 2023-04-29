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
        private InputEventPartialProvider _inputEventPartialProvider;

        private Configuration _cfg;

        private InputActionAsset _inputActionAsset;
        private InputActionReference _pointAction;
        private InputActionReference _moveAction;
        private InputActionReference _submitAction;
        private InputActionReference _cancelAction;
        private InputActionReference _leftClickAction;
        private InputActionReference _middleClickAction;
        private InputActionReference _rightClickAction;
        private InputActionReference _scrollWheelAction;

        InputAction _nextPreviousAction;

        List<Event> _events = new List<Event>();

        private PointerState _mouseState;
        private bool _seenMouseEvents;

        private PointerState _penState;
        private bool _seenPenEvents;

        private PointerState _touchState;
        private bool _seenTouchEvents;

        private const float kSmallestReportedMovementSqrDist = 0.01f;

        private NavigationEventRepeatHelper repeatHelper = new();

        static InputSystemProvider()
        {
            // Only if InputSystem is enabled in the PlayerSettings do we set it as the provider.
            // This includes situations where both InputManager and InputSystem are enabled.
#if ENABLE_INPUT_SYSTEM
            EventProvider.SetInputSystemProvider(new InputSystemProvider());
#endif
        }

        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Bootstrap() {} // Empty function. Exists only to invoke the static class constructor in Runtime Players

        private EventModifiers _eventModifiers => _inputEventPartialProvider._eventModifiers;

        private DiscreteTime _currentTime => (DiscreteTime)Time.timeAsRational;

        private const uint kDefaultPlayerId = 0u;

        public void Initialize()
        {
            _inputEventPartialProvider ??= new InputEventPartialProvider();
            _inputEventPartialProvider.Initialize();

            _events.Clear();

            _mouseState.Reset();
            _seenMouseEvents = false;

            _penState.Reset();
            _seenPenEvents = false;

            _touchState.Reset();
            _seenTouchEvents = false;

            // TODO should UITK somehow override this?
            _cfg = Configuration.GetDefaultConfiguration();
            RegisterActions(_cfg);
        }

        public void Shutdown()
        {
            UnregisterActions(_cfg);

            _inputEventPartialProvider.Shutdown();
            _inputEventPartialProvider = null;
        }

        public void Update()
        {
            _inputEventPartialProvider.Update();

            _events.Sort(SortEvents);

            var currentTime = (DiscreteTime)Time.timeAsRational;

            DirectionNavigation(currentTime);

            foreach (var ev in _events)
            {
                // we need to ignore some pointer events based on priority (touch->pen->mouse)
                // this is mostly used to filter out simulated input, e.g. when pen is active it also generates mouse input
                if (_seenTouchEvents && ev.type == Event.Type.PointerEvent && ev.eventSource == EventSource.Pen)
                    _penState.Reset();
                else if ((_seenTouchEvents || _seenPenEvents) && ev.type == Event.Type.PointerEvent && (ev.eventSource == EventSource.Mouse || ev.eventSource == EventSource.Unspecified))
                    _mouseState.Reset();
                else
                    EventProvider.Dispatch(ev);
            }

            _events.Clear();

            _seenTouchEvents = false;
            _seenPenEvents = false;
            _seenMouseEvents = false;
        }

        //TODO: Refactor as there is no need for having almost the same implementation in the IM and ISX?
        private void DirectionNavigation(DiscreteTime currentTime)
        {
            var(move, axesButtonWerePressed) = ReadCurrentNavigationMoveVector();
            var direction = NavigationEvent.DetermineMoveDirection(move);

            // Checks for next/previous directions if no movement was detected
            if (direction == NavigationEvent.Direction.None)
            {
                direction = ReadNextPreviousDirection();
                axesButtonWerePressed = _nextPreviousAction.WasPressedThisFrame();
            }

            if (direction == NavigationEvent.Direction.None)
            {
                repeatHelper.Reset();
            }
            else
            {
                if (repeatHelper.ShouldSendMoveEvent(currentTime, direction, axesButtonWerePressed))
                {
                    EventProvider.Dispatch(Event.From(new NavigationEvent
                    {
                        type = NavigationEvent.Type.Move,
                        direction = direction,
                        timestamp = currentTime,
                        eventSource = GetEventSource(GetActiveDeviceFromDirection(direction)),
                        playerId = kDefaultPlayerId,
                        eventModifiers = _eventModifiers
                    }));
                }
            }
        }

        private InputDevice GetActiveDeviceFromDirection(NavigationEvent.Direction direction)
        {
            switch (direction)
            {
                case NavigationEvent.Direction.Left:
                case NavigationEvent.Direction.Up:
                case NavigationEvent.Direction.Right:
                case NavigationEvent.Direction.Down:
                    return _moveAction.action.activeControl.device;
                case NavigationEvent.Direction.Next:
                case NavigationEvent.Direction.Previous:
                    return _nextPreviousAction.activeControl.device;
                case NavigationEvent.Direction.None:
                default:
                    return Keyboard.current;
            }
        }

        private (Vector2, bool) ReadCurrentNavigationMoveVector()
        {
            var move = _moveAction.action.ReadValue<Vector2>();
            // Check if the action was "pressed" this frame to deal with repeating events
            var axisWasPressed = _moveAction.action.WasPressedThisFrame();
            return (move, axisWasPressed);
        }

        private NavigationEvent.Direction ReadNextPreviousDirection()
        {
            if (_nextPreviousAction.IsPressed())
            {
                //TODO: For now it only deals with Keyboard, needs to deal with other devices if we can add bindings
                //      for Gamepad, etc
                //TODO: An alternative could be to have an action for next and for previous since shortcut support does
                //      not work properly
                if (_nextPreviousAction.activeControl.device is Keyboard)
                {
                    var keyboard = _nextPreviousAction.activeControl.device as Keyboard;
                    // Return direction based on whether shift is pressed or not
                    return keyboard.shiftKey.isPressed ? NavigationEvent.Direction.Previous : NavigationEvent.Direction.Next;
                }
            }

            return NavigationEvent.Direction.None;
        }

        private static int SortEvents(Event a, Event b)
        {
            return Event.CompareType(a, b);
        }

        public void OnFocusChanged(bool focus)
        {
            _inputEventPartialProvider.OnFocusChanged(focus);
        }

        public bool RequestCurrentState(Event.Type type)
        {
            if (_inputEventPartialProvider.RequestCurrentState(type))
                return true;

            switch (type)
            {
                case Event.Type.PointerEvent:
                {
                    if (_touchState.LastPositionValid)
                        EventProvider.Dispatch(Event.From(ToPointerStateEvent(_currentTime, _touchState, EventSource.Touch)));
                    if (_penState.LastPositionValid)
                        EventProvider.Dispatch(Event.From(ToPointerStateEvent(_currentTime, _penState, EventSource.Pen)));
                    if (_mouseState.LastPositionValid)
                        EventProvider.Dispatch(Event.From(ToPointerStateEvent(_currentTime, _mouseState, EventSource.Mouse)));
                    else
                    {
                        // TODO maybe it's reasonable to poll and dispatch mouse state here anyway?
                    }

                    return _touchState.LastPositionValid ||
                        _penState.LastPositionValid ||
                        _mouseState.LastPositionValid;
                }
                // TODO
                case Event.Type.IMECompositionEvent:
                //EventProvider.Dispatch(Event.From(ToIMECompositionEvent(currentTime, _compositionString)));
                //return true;
                default:
                    return false;
            }
        }

        public uint playerCount => 1; // TODO

        // copied from UIElementsRuntimeUtility.cs
        private static Vector2 ScreenBottomLeftToPanelPosition(Vector2 position, int targetDisplay)
        {
            // Flip positions Y axis between input and UITK
            var screenHeight = Screen.height;
            if (targetDisplay > 0 && targetDisplay < Display.displays.Length)
                screenHeight = Display.displays[targetDisplay].systemHeight;
            position.y = screenHeight - position.y;
            return position;
        }

        private PointerEvent ToPointerStateEvent(DiscreteTime currentTime, in PointerState state, EventSource eventSource)
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
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            };
        }

        private EventSource GetEventSource(InputAction.CallbackContext ctx)
        {
            var device = ctx.control.device;
            return GetEventSource(device);
        }

        private EventSource GetEventSource(InputDevice device)
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

        private ref PointerState GetPointerStateForSource(EventSource eventSource)
        {
            switch (eventSource)
            {
                case EventSource.Touch:
                    return ref _touchState;
                case EventSource.Pen:
                    return ref _penState;
                default:
                    return ref _mouseState;
            }
        }

        private void DispatchFromCallback(in Event ev)
        {
            _events.Add(ev);
        }

        private void OnPointerPerformed(InputAction.CallbackContext ctx)
        {
            var eventSource = GetEventSource(ctx);
            ref var pointerState = ref GetPointerStateForSource(eventSource);

            // Overall I'm not happy how leaky this is, we're using input actions to have flexibility to bind to different controls,
            // but instead we just kinda abuse it to bind to different devices ...
            var asPointerDevice = ctx.control.device is Pointer ? (Pointer)ctx.control.device : null;
            var asPenDevice = ctx.control.device is Pen ? (Pen)ctx.control.device : null;
            var asTouchscreenDevice = ctx.control.device is Touchscreen ? (Touchscreen)ctx.control.device : null;
            var asTouchControl = ctx.control is TouchControl ? (TouchControl)ctx.control : null;
            var pointerIndex = FindPointerIndex(asTouchscreenDevice, asTouchControl);

            if (asTouchControl != null || asTouchscreenDevice != null)
                _seenTouchEvents = true;
            else if (asPenDevice != null)
                _seenPenEvents = true;
            else
                _seenMouseEvents = true;

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

            if (delta.sqrMagnitude >= kSmallestReportedMovementSqrDist)
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
                    timestamp = _currentTime,
                    eventSource = eventSource,
                    playerId = kDefaultPlayerId,
                    eventModifiers = _eventModifiers
                }));

                // only record if we send an event
                pointerState.OnMove(_currentTime, position, targetDisplay);
            }
            else if (!pointerState.LastPositionValid)
                pointerState.OnMove(_currentTime, position, targetDisplay);
        }

        private void OnSubmitPerformed(InputAction.CallbackContext ctx)
        {
            DispatchFromCallback(Event.From(new NavigationEvent
            {
                type = NavigationEvent.Type.Submit,
                direction = NavigationEvent.Direction.None,
                timestamp = _currentTime,
                eventSource = GetEventSource(ctx),
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            }));
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            DispatchFromCallback(Event.From(new NavigationEvent
            {
                type = NavigationEvent.Type.Cancel,
                direction = NavigationEvent.Direction.None,
                timestamp = _currentTime,
                eventSource = GetEventSource(ctx),
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            }));
        }

        private int FindPointerIndex(Touchscreen touchscreen, TouchControl touchControl)
        {
            if (touchscreen == null || touchControl == null)
                return 0;

            for (var i = 0; i < touchscreen.touches.Count; ++i)
                if (touchscreen.touches[i] == touchControl)
                    return i;

            return 0;
        }

        private void OnClickPerformed(InputAction.CallbackContext ctx, EventSource eventSource, PointerEvent.Button button)
        {
            ref var state = ref GetPointerStateForSource(eventSource);

            var asTouchscreenDevice = ctx.control.device is Touchscreen ? (Touchscreen)ctx.control.device : null;
            var asTouchControl = ctx.control is TouchControl ? (TouchControl)ctx.control : null;
            var pointerIndex = FindPointerIndex(asTouchscreenDevice, asTouchControl);
            
            if (asTouchControl != null || asTouchscreenDevice != null)
                _seenTouchEvents = true;
            else
                _seenMouseEvents = true;

            var wasPressed = state.ButtonsState.Get(button);
            var isPressed = ctx.ReadValueAsButton();
            state.OnButtonChange(_currentTime, button, wasPressed, isPressed);

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
                timestamp = _currentTime,
                eventSource = eventSource,
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            }));
        }

        private void OnLeftClickPerformed(InputAction.CallbackContext ctx) => OnClickPerformed(ctx, GetEventSource(ctx), PointerEvent.Button.MouseLeft);
        private void OnMiddleClickPerformed(InputAction.CallbackContext ctx) => OnClickPerformed(ctx, GetEventSource(ctx), PointerEvent.Button.MouseMiddle);
        private void OnRightClickPerformed(InputAction.CallbackContext ctx) => OnClickPerformed(ctx, GetEventSource(ctx), PointerEvent.Button.MouseRight);

        private void OnScrollWheelPerformed(InputAction.CallbackContext ctx)
        {
            var scrollDelta = ctx.ReadValue<Vector2>();
            if (scrollDelta.sqrMagnitude < kSmallestReportedMovementSqrDist)
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
            // TODO check how it behaves on macOS
            scrollDelta.x /= 40.0f;
            scrollDelta.y /= -40.0f;

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
                timestamp = _currentTime,
                eventSource = EventSource.Mouse,
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            }));
        }

        private void RegisterNextPreviousAction()
        {
            _nextPreviousAction = new InputAction(name: "nextPreviousAction", type: InputActionType.Button);
            // TODO add more default bindings, or make them configurable
            _nextPreviousAction.AddBinding("<Keyboard>/tab");
            _nextPreviousAction.Enable();
        }

        private void UnregisterNextPreviousAction()
        {
            if (_nextPreviousAction != null)
            {
                _nextPreviousAction.Disable();
                _nextPreviousAction = null;
            }
        }

        private void RegisterActions(Configuration cfg)
        {
            _inputActionAsset = InputActionAsset.FromJson(cfg.InputActionAssetAsJson);

            _pointAction = InputActionReference.Create(_inputActionAsset.FindAction(_cfg.PointAction));
            _moveAction = InputActionReference.Create(_inputActionAsset.FindAction(_cfg.MoveAction));
            _submitAction = InputActionReference.Create(_inputActionAsset.FindAction(_cfg.SubmitAction));
            _cancelAction = InputActionReference.Create(_inputActionAsset.FindAction(_cfg.CancelAction));
            _leftClickAction = InputActionReference.Create(_inputActionAsset.FindAction(_cfg.LeftClickAction));
            _middleClickAction = InputActionReference.Create(_inputActionAsset.FindAction(_cfg.MiddleClickAction));
            _rightClickAction = InputActionReference.Create(_inputActionAsset.FindAction(_cfg.RightClickAction));
            _scrollWheelAction = InputActionReference.Create(_inputActionAsset.FindAction(_cfg.ScrollWheelAction));

            if (_pointAction.action != null)
                _pointAction.action.performed += OnPointerPerformed;

            if (_submitAction.action != null)
                _submitAction.action.performed += OnSubmitPerformed;

            if (_cancelAction.action != null)
                _cancelAction.action.performed += OnCancelPerformed;

            if (_leftClickAction.action != null)
                _leftClickAction.action.performed += OnLeftClickPerformed;

            if (_middleClickAction.action != null)
                _middleClickAction.action.performed += OnMiddleClickPerformed;

            if (_rightClickAction.action != null)
                _rightClickAction.action.performed += OnRightClickPerformed;

            if (_scrollWheelAction.action != null)
                _scrollWheelAction.action.performed += OnScrollWheelPerformed;

            // When adding new one's don't forget to add them to UnregisterActions

            _inputActionAsset.Enable();

            // TODO make it configurable as it is not part of default config
            // The Next/Previous action is not part of the input actions asset
            RegisterNextPreviousAction();
        }

        private void UnregisterActions(Configuration cfg)
        {
            if (_pointAction.action != null)
                _pointAction.action.performed -= OnPointerPerformed;

            if (_submitAction.action != null)
                _submitAction.action.performed -= OnSubmitPerformed;

            if (_cancelAction.action != null)
                _cancelAction.action.performed -= OnCancelPerformed;

            if (_leftClickAction.action != null)
                _leftClickAction.action.performed -= OnLeftClickPerformed;

            if (_middleClickAction.action != null)
                _middleClickAction.action.performed -= OnMiddleClickPerformed;

            if (_rightClickAction.action != null)
                _rightClickAction.action.performed -= OnRightClickPerformed;

            if (_scrollWheelAction.action != null)
                _scrollWheelAction.action.performed -= OnScrollWheelPerformed;

            _pointAction = null;
            _moveAction = null;
            _submitAction = null;
            _cancelAction = null;
            _leftClickAction = null;
            _middleClickAction = null;
            _rightClickAction = null;
            _scrollWheelAction = null;

            _inputActionAsset.Disable();

            // The Next/Previous action is not part of the input actions asset
            UnregisterNextPreviousAction();

            UnityEngine.Object.Destroy(_inputActionAsset); // TODO check if this is ok
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
            //public string TrackedDevicePositionAction;
            //public string TrackedDeviceOrientationAction;

            // public float InputActionsPerSecond;
            // public float RepeatDelay;

            public static Configuration GetDefaultConfiguration()
            {
                // TODO this is a weird way of doing that, is there an easier way?
                var asset = new DefaultInputActions();
                var json = asset.asset.ToJson();
                UnityEngine.Object.DestroyImmediate(asset.asset); // TODO just Dispose doesn't work in edit mode
                // asset.Dispose();

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
                    // InputActionsPerSecond = 10,
                    // RepeatDelay = 0.5f,
                };
            }
        }
    }
}
#endif // UNITY_2023_2_OR_NEWER
