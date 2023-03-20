using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.IntegerTime;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputForUI;
using UnityEngine.InputSystem;
using Event = UnityEngine.InputForUI.Event;
using EventModifiers = UnityEngine.InputForUI.EventModifiers;
using EventProvider = UnityEngine.InputForUI.EventProvider;

namespace InputSystem.Plugins.InputForUI
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal class InputSystemProvider : IEventProviderImpl
    {
        private InputEventPartialProvider _inputEventPartialProvider;

        private Configuration _cfg;

        private List<Event> _events = new List<Event>();

        static InputSystemProvider()
        {
            // TODO check if input system is enabled before doing this
            // enable me to test!
            // EventProvider.SetInputSystemProvider(new InputSystemProvider());
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
            if (cfg.PointAction.action != null)
                cfg.PointAction.action.performed += OnPointerPerformed;

            if (cfg.MoveAction.action != null)
                cfg.MoveAction.action.performed += OnMovePerformed;
            
            if (cfg.SubmitAction.action != null)
                cfg.SubmitAction.action.performed += OnSubmitPerformed;

            if (cfg.CancelAction.action != null)
                cfg.CancelAction.action.performed += OnCancelPerformed;

            if (cfg.LeftClickAction.action != null)
                cfg.LeftClickAction.action.performed += OnLeftClickPerformed;

            if (cfg.MiddleClickAction.action != null)
                cfg.MiddleClickAction.action.performed += OnMiddleClickPerformed;

            if (cfg.RightClickAction.action != null)
                cfg.RightClickAction.action.performed += OnRightClickPerformed;

            if (cfg.ScrollWheelAction.action != null)
                cfg.ScrollWheelAction.action.performed += OnScrollWheelPerformed;
            
            // When adding new one's don't forget to add them to UnregisterActions 

            cfg.InputActionAsset.Enable();
        }

        private void UnregisterActions(Configuration cfg)
        {
            if (cfg.PointAction.action != null)
                cfg.PointAction.action.performed -= OnPointerPerformed;
            
            if (cfg.MoveAction.action != null)
                cfg.MoveAction.action.performed -= OnMovePerformed;
            
            if (cfg.SubmitAction.action != null)
                cfg.SubmitAction.action.performed -= OnSubmitPerformed;

            if (cfg.CancelAction.action != null)
                cfg.CancelAction.action.performed -= OnCancelPerformed;

            if (cfg.LeftClickAction.action != null)
                cfg.LeftClickAction.action.performed -= OnLeftClickPerformed;

            if (cfg.MiddleClickAction.action != null)
                cfg.MiddleClickAction.action.performed -= OnMiddleClickPerformed;

            if (cfg.RightClickAction.action != null)
                cfg.RightClickAction.action.performed -= OnRightClickPerformed;

            if (cfg.ScrollWheelAction.action != null)
                cfg.ScrollWheelAction.action.performed -= OnScrollWheelPerformed;

            cfg.InputActionAsset.Disable();
        }

        public struct Configuration
        {
            public InputActionAsset InputActionAsset;
            public InputActionReference PointAction;
            public InputActionReference MoveAction;
            public InputActionReference SubmitAction;
            public InputActionReference CancelAction;
            public InputActionReference LeftClickAction;
            public InputActionReference MiddleClickAction;
            public InputActionReference RightClickAction;
            public InputActionReference ScrollWheelAction;
            //public InputActionReference TrackedDevicePositionAction;
            //public InputActionReference TrackedDeviceOrientationAction;

            // public float InputActionsPerSecond;
            // public float RepeatDelay;

            public IEnumerable<InputActionReference> InputActionReferences()
            {
                yield return PointAction;
            }

            public static Configuration GetDefaultConfiguration()
            {
                // TODO doesn't work in player builds
                //var asset = (InputActionAsset)AssetDatabase.LoadAssetAtPath("Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions", typeof(InputActionAsset));
                var asset = new DefaultInputActions().asset;
                
                return new Configuration
                {
                    InputActionAsset = asset,
                    PointAction = InputActionReference.Create(asset.FindAction("UI/Point")),
                    MoveAction = InputActionReference.Create(asset.FindAction("UI/Navigate")),
                    SubmitAction = InputActionReference.Create(asset.FindAction("UI/Submit")),
                    CancelAction = InputActionReference.Create(asset.FindAction("UI/Cancel")),
                    LeftClickAction = InputActionReference.Create(asset.FindAction("UI/Click")),
                    MiddleClickAction = InputActionReference.Create(asset.FindAction("UI/MiddleClick")),
                    RightClickAction = InputActionReference.Create(asset.FindAction("UI/RightClick")),
                    ScrollWheelAction = InputActionReference.Create(asset.FindAction("UI/ScrollWheel")),
                    // InputActionsPerSecond = 10,
                    // RepeatDelay = 0.5f,
                };
            }
        }
    }
}
