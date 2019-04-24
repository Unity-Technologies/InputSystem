using System;
using UnityEngine.Experimental.Input.Plugins.PlayerInput;

namespace UnityEngine.Experimental.Input.Plugins.UI
{
    /// <summary>
    /// Input module that takes its input from a PlayerInput component.
    /// </summary>
    public class UIPlayerInputModule : UIInputModule
    {
        private void AddListenerIfMatching(PlayerInput.PlayerInput.ActionEvent actionEvent, InputAction action, Events.UnityAction<InputAction.CallbackContext> callback)
        {
            if (action != null && actionEvent.actionId == action.id.ToString())
            {
                actionEvent.AddListener(callback);
                action.Enable();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_PlayerInput == null)
                m_PlayerInput = GetComponent<PlayerInput.PlayerInput>();
            if (m_PlayerInput == null)
            {
                Debug.LogError("UIPlayerInputModule needs a PlayerInput component to work, but there is non assigned to it, and none present on the current game object!");
                return;
            }
            player.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
            var actions = player.actions;
            if (actions == null)
            {
                Debug.LogError("UIPlayerInputModule needs a PlayerInput component with an Input Action Asset assigned to it.");
                return;
            }
            var pointAction = actions.FindAction("Point");
            var clickAction = actions.FindAction("Click");
            var rightClickAction = actions.FindAction("RightClick");
            var scrollAction = actions.FindAction("Scroll");
            var navigateAction = actions.FindAction("Navigate");
            var cancelAction = actions.FindAction("Cancel");
            var submitAction = actions.FindAction("Submit");
            if ((pointAction == null || clickAction == null) && (navigateAction == null || submitAction == null))
                Debug.LogError("UIPlayerInputModule needs a PlayerInput component with an Input Action Asset which implements at least a `Point` action and a `Click` action or a `Navigate` action and a `Submit` action.");

            foreach (var evt in player.actionEvents)
            {
                AddListenerIfMatching(evt, pointAction, OnPoint);
                AddListenerIfMatching(evt, clickAction, OnClick);
                AddListenerIfMatching(evt, rightClickAction, OnRightClick);
                AddListenerIfMatching(evt, navigateAction, OnNavigate);
                AddListenerIfMatching(evt, cancelAction, OnCancel);
                AddListenerIfMatching(evt, submitAction, OnSubmit);
                AddListenerIfMatching(evt, scrollAction, OnScroll);
            }
        }

        private PlayerInput.PlayerInput player => m_PlayerInput;

        public void OnPoint(InputAction.CallbackContext context)
        {
            mouseState.position = context.ReadValue<Vector2>();
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            var buttonState = mouseState.leftButton;
            buttonState.isDown = context.ReadValue<float>() > 0;
            mouseState.leftButton = buttonState;
        }

        public void OnRightClick(InputAction.CallbackContext context)
        {
            var buttonState = mouseState.rightButton;
            buttonState.isDown = context.ReadValue<float>() > 0;
            mouseState.rightButton = buttonState;
        }

        public void OnScroll(InputAction.CallbackContext context)
        {
            mouseState.scrollPosition = context.ReadValue<Vector2>();
        }

        public void OnNavigate(InputAction.CallbackContext context)
        {
            joystickState.move = context.ReadValue<Vector2>();
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            joystickState.cancelButtonDown = context.ReadValue<float>() > 0;
        }

        public void OnSubmit(InputAction.CallbackContext context)
        {
            joystickState.submitButtonDown = context.ReadValue<float>() > 0;
        }

        public override void Process()
        {
            ProcessJoystick(ref joystickState);
            ProcessMouse(ref mouseState);
        }

        public PlayerInput.PlayerInput m_PlayerInput;

        [NonSerialized] private MouseModel mouseState;
        [NonSerialized] private JoystickModel joystickState;
    }
}
