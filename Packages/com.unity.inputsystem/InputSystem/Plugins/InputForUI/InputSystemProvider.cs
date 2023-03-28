using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.InputForUI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Event = UnityEngine.InputForUI.Event;
using EventModifiers = UnityEngine.InputForUI.EventModifiers;
using EventProvider = UnityEngine.InputForUI.EventProvider;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InputSystem.Plugins.InputForUI
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
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

        private List<Event> _events = new List<Event>();

        private PointerState _mouseState;
        private bool _seenMouseEvents;

        private PointerState _penState;
        private bool _seenPenEvents;

        private Dictionary<int, int> _touchFingerIdToFingerIndex = new();
        private int _touchNextFingerIndex;
        private int _aliveTouchesCount;
        private PointerState _touchState;
        private bool _seenTouchEvents;

        private const float kSmallestReportedMovementSqrDist = 0.01f;

        static InputSystemProvider()
        {
            // TODO check if input system is enabled before doing this
            // enable me to test!
            EventProvider.SetInputSystemProvider(new InputSystemProvider());
        }

        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Bootstrap()
        {
            // will invoke static class constructor
        }

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
            
            _touchFingerIdToFingerIndex.Clear();
            _touchNextFingerIndex = 0;
            _aliveTouchesCount = 0;
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

            // it's very difficult to calculate is all touches are released to reset the counter
            // so we proactively guarding for worst cases when either we get more cancellations then we expect,
            // or something gets stuck and alive touches get increment beyond any reasonable values
            if (_aliveTouchesCount <= 0 || _aliveTouchesCount >= 16) // safety guard
            {
                _touchNextFingerIndex = 0;
                _aliveTouchesCount = 0;
                _touchFingerIdToFingerIndex.Clear();
            }
        }

        private static int SortEvents(Event a, Event b)
        {
            return Event.Compare(a, b);
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
        
        private EventSource GetEventSourceForCallback(InputAction.CallbackContext ctx)
        {
            var device = ctx.control.device;

            if (device is Touchscreen)
                return EventSource.Touch;
            if (device is Pen)
                return EventSource.Pen;
            if (device is Mouse)
                return EventSource.Mouse;
            if (device is Keyboard)
                return EventSource.Keyboard;
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
            var eventSource = GetEventSourceForCallback(ctx);
            ref var pointerState = ref GetPointerStateForSource(eventSource);
            
            // Overall I'm not happy how leaky this is, we're using input actions to have flexibility to bind to different controls,
            // but instead we just kinda abuse it to bind to different devices ...
            var asPointerDevice = ctx.control.device is Pointer ? (Pointer)ctx.control.device : null;
            var asPenDevice = ctx.control.device is Pen ? (Pen)ctx.control.device : null;
            var asTouchscreenDevice = ctx.control.device is Touchscreen ? (Touchscreen)ctx.control.device : null;
            var asTouchControl = ctx.control is TouchControl ? (TouchControl)ctx.control : null;

            if (asTouchControl != null)
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
                    pointerIndex = asTouchControl != null ? asTouchControl.touchId.ReadValue() : 0,
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
            else if(!pointerState.LastPositionValid)
                pointerState.OnMove(_currentTime, position, targetDisplay);
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            var direction = NavigationEvent.DetermineMoveDirection(ctx.ReadValue<Vector2>());
            if (direction == NavigationEvent.Direction.None)
                return;
            //     _navigationEventRepeatHelper.Reset();

            // TODO repeat rate
            DispatchFromCallback(Event.From(new NavigationEvent
            {
                type = NavigationEvent.Type.Move,
                direction = direction,
                timestamp = _currentTime,
                eventSource = EventSource.Unspecified, // TODO
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            }));
        }

        private void OnSubmitPerformed(InputAction.CallbackContext ctx)
        {
            // TODO repeat rate
            DispatchFromCallback(Event.From(new NavigationEvent
            {
                type = NavigationEvent.Type.Submit,
                direction = NavigationEvent.Direction.None,
                timestamp = _currentTime,
                eventSource = EventSource.Unspecified, // TODO
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            }));
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            // TODO repeat rate
            DispatchFromCallback(Event.From(new NavigationEvent
            {
                type = NavigationEvent.Type.Cancel,
                direction = NavigationEvent.Direction.None,
                timestamp = _currentTime,
                eventSource = EventSource.Unspecified, // TODO
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            }));
        }

        private void OnClickPerformed(InputAction.CallbackContext ctx, EventSource eventSource, PointerEvent.Button button)
        {
            ref var state = ref GetPointerStateForSource(eventSource);

            var asTouchControl = ctx.control is TouchControl ? (TouchControl)ctx.control : null;
            var touchId = asTouchControl != null ? asTouchControl.touchId.ReadValue() : 0;

            var pointerIndex = 0;
            if (asTouchControl != null && !_touchFingerIdToFingerIndex.TryGetValue(touchId, out pointerIndex))
            {
                pointerIndex = _touchNextFingerIndex++;
                _aliveTouchesCount++;
                _touchFingerIdToFingerIndex.Add(touchId, pointerIndex);
            }

            var wasPressed = state.ButtonsState.Get(button);
            var isPressed = ctx.ReadValueAsButton();
            state.OnButtonChange(_currentTime, button, wasPressed, isPressed);

            if (asTouchControl != null && wasPressed && !isPressed)
                _aliveTouchesCount--;

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

        private void OnClickCancelled(InputAction.CallbackContext ctx, EventSource eventSource, PointerEvent.Button button)
        {
            ref var state = ref GetPointerStateForSource(eventSource);

            var asTouchControl = ctx.control is TouchControl ? (TouchControl)ctx.control : null;
            var touchId = asTouchControl != null ? asTouchControl.touchId.ReadValue() : 0;

            var wasPressed = state.ButtonsState.Get(button);
            var isPressed = ctx.ReadValueAsButton();
            
            if (asTouchControl != null && wasPressed && !isPressed && _touchFingerIdToFingerIndex.ContainsKey(touchId))
                _aliveTouchesCount--;
        }

        private void OnLeftClickPerformed(InputAction.CallbackContext ctx) => OnClickPerformed(ctx, GetEventSourceForCallback(ctx), PointerEvent.Button.MouseLeft);
        
        private void OnLeftClickCancelled(InputAction.CallbackContext ctx) => OnClickCancelled(ctx, GetEventSourceForCallback(ctx), PointerEvent.Button.MouseLeft);

        private void OnMiddleClickPerformed(InputAction.CallbackContext ctx) => OnClickPerformed(ctx, GetEventSourceForCallback(ctx), PointerEvent.Button.MouseMiddle);
        private void OnMiddleClickCancelled(InputAction.CallbackContext ctx) => OnClickCancelled(ctx, GetEventSourceForCallback(ctx), PointerEvent.Button.MouseLeft);

        private void OnRightClickPerformed(InputAction.CallbackContext ctx) => OnClickPerformed(ctx, GetEventSourceForCallback(ctx), PointerEvent.Button.MouseRight);
        private void OnRightClickCancelled(InputAction.CallbackContext ctx) => OnClickCancelled(ctx, GetEventSourceForCallback(ctx), PointerEvent.Button.MouseLeft);

        private void OnScrollWheelPerformed(InputAction.CallbackContext ctx)
        {
            var scrollDelta = ctx.ReadValue<Vector2>();
            if (scrollDelta.sqrMagnitude < kSmallestReportedMovementSqrDist)
                return;

            var eventSource = GetEventSourceForCallback(ctx);
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

            if (_moveAction.action != null)
                _moveAction.action.performed += OnMovePerformed;
            
            if (_submitAction.action != null)
                _submitAction.action.performed += OnSubmitPerformed;

            if (_cancelAction.action != null)
                _cancelAction.action.performed += OnCancelPerformed;

            if (_leftClickAction.action != null)
            {
                _leftClickAction.action.performed += OnLeftClickPerformed;
                _leftClickAction.action.canceled += OnLeftClickCancelled;
            }

            if (_middleClickAction.action != null)
            {
                _middleClickAction.action.performed += OnMiddleClickPerformed;
                _middleClickAction.action.canceled += OnMiddleClickCancelled;
            }

            if (_rightClickAction.action != null)
            {
                _rightClickAction.action.performed += OnRightClickPerformed;
                _rightClickAction.action.canceled += OnRightClickCancelled;
            }

            if (_scrollWheelAction.action != null)
                _scrollWheelAction.action.performed += OnScrollWheelPerformed;
            
            // When adding new one's don't forget to add them to UnregisterActions 

            _inputActionAsset.Enable();
        }

        private void UnregisterActions(Configuration cfg)
        {
            if (_pointAction.action != null)
                _pointAction.action.performed -= OnPointerPerformed;
            
            if (_moveAction.action != null)
                _moveAction.action.performed -= OnMovePerformed;
            
            if (_submitAction.action != null)
                _submitAction.action.performed -= OnSubmitPerformed;

            if (_cancelAction.action != null)
                _cancelAction.action.performed -= OnCancelPerformed;

            if (_leftClickAction.action != null)
            {
                _leftClickAction.action.performed -= OnLeftClickPerformed;
                _leftClickAction.action.canceled -= OnLeftClickCancelled;
            }

            if (_middleClickAction.action != null)
            {
                _middleClickAction.action.performed -= OnMiddleClickPerformed;
                _middleClickAction.action.canceled -= OnMiddleClickCancelled;
            }

            if (_rightClickAction.action != null)
            {
                _rightClickAction.action.performed -= OnRightClickPerformed;
                _rightClickAction.action.canceled -= OnRightClickCancelled;
            }

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
