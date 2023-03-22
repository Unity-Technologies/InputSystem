using System.Collections.Generic;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.InputForUI;
using UnityEngine.InputSystem;
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

            foreach (var ev in _events) // TODO sort them
                EventProvider.Dispatch(ev);
            _events.Clear();
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
                // TODO
                default:
                    return false;
            }
        }

        public uint playerCount => 1; // TODO
        
        private void DispatchFromCallback(in Event ev)
        {
            _events.Add(ev);
        }

        private void OnPointerPerformed(InputAction.CallbackContext ctx)
        {
            // Debug.Log($"Pointer performed {ctx.control.name}");

            var position = ctx.ReadValue<Vector2>();
            var delta = Vector2.zero;
            var targetDisplay = 0;

            DispatchFromCallback(Event.From(new PointerEvent
            {
                type = PointerEvent.Type.PointerMoved,
                pointerIndex = 0,
                position = position,
                deltaPosition = delta,
                scroll = Vector2.zero,
                displayIndex = targetDisplay,
                tilt = Vector2.zero,
                twist = 0.0f,
                pressure = 0.0f,
                isInverted = false,
                button = 0,
                //buttonsState = _mouseState.ButtonsState,
                clickCount = 0,
                timestamp = _currentTime,
                eventSource = EventSource.Mouse,
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            }));
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
        
        private void OnLeftClickPerformed(InputAction.CallbackContext ctx)
        {
            
//             var index = GetPointerStateIndexFor(ref context);
//             if (index == -1)
//                 return;
//
//             ref var state = ref GetPointerStateForIndex(index);
//             bool wasPressed = state.leftButton.isPressed;
//             state.leftButton.isPressed = context.ReadValueAsButton();
//             state.changedThisFrame = true;
//             if (IgnoreNextClick(ref context, wasPressed))
//                 state.leftButton.ignoreNextClick = true;
// #if UNITY_2023_1_OR_NEWER
//             state.eventData.displayIndex = GetDisplayIndexFor(context.control);
// #endif
//             
        }

        private void OnMiddleClickPerformed(InputAction.CallbackContext ctx)
        {
        }

        private void OnRightClickPerformed(InputAction.CallbackContext ctx)
        {
        }

        private void OnScrollWheelPerformed(InputAction.CallbackContext ctx)
        {
            // TODO
            var position = Vector2.zero;
            var delta = Vector2.zero;
            var scroll = ctx.ReadValue<Vector2>();
            var targetDisplay = 0;

            DispatchFromCallback(Event.From(new PointerEvent
            {
                type = PointerEvent.Type.Scroll,
                pointerIndex = 0,
                position = position,
                deltaPosition = delta,
                scroll = scroll,
                displayIndex = targetDisplay,
                tilt = Vector2.zero,
                twist = 0.0f,
                pressure = 0.0f,
                isInverted = false,
                button = 0,
                //buttonsState = _mouseState.ButtonsState,
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
                _leftClickAction.action.performed += OnLeftClickPerformed;

            if (_middleClickAction.action != null)
                _middleClickAction.action.performed += OnMiddleClickPerformed;

            if (_rightClickAction.action != null)
                _rightClickAction.action.performed += OnRightClickPerformed;

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
