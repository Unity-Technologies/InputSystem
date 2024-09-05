using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.Serialization;
using Gamepad = UnityEngine.InputSystem.Experimental.Devices.Gamepad;
using Keyboard = UnityEngine.InputSystem.Experimental.Devices.Keyboard;

namespace UseCases
{
    public class UseCaseBindingAsset : UseCase
    {
        #region How to currently do it
        /*
        [SerializeField] public InputActionReference m_MoveActionReference;
        private InputAction m_MoveAction;
        private void OnEnable()
        {
            if (m_MoveActionReference != null && m_MoveActionReference.action != null)
                m_MoveAction = m_MoveActionReference.action;
            else
                m_MoveAction = new InputAction("Move", InputActionType.PassThrough);
            if (m_MoveAction.bindings.Count == 0)
            {
                m_MoveAction.AddCompositeBinding("Dpad")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                m_MoveAction.AddBinding("<Gamepad>/leftStick");
            }
            m_MoveAction.performed += OnMove;


            if (m_Move.bindingCount == 0)
            {
                m_Move.AddBinding(Combine.Composite(Keyboard.A, Keyboard.D, Keyboard.S, Keyboard.W));
                m_Move.AddBinding(Gamepad.leftStick);
            }
            
            m_Subscription = m_Move.Subscribe(v => moveDirection = v);
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            moveDirection = ctx.ReadValue<Vector2>();
        }

        void OnDisable()
        {
            m_MoveAction.performed -= OnMove;
        }
        */
        #endregion

        public BindableInput<Vector2> m_Move = new BindableInput<Vector2>(); // TODO Should be a scriptable object or extracted from one instead of new

        public BindableInput<InputEvent> m_Celebrate;
        
        private IDisposable m_Subscription;
    
        private void OnEnable()
        {
            if (m_Move.bindingCount == 0)
            {
                m_Move.AddBinding(Combine.Composite(Keyboard.A, Keyboard.D, Keyboard.S, Keyboard.W));
                m_Move.AddBinding(Gamepad.leftStick);
            }
            
            m_Subscription = m_Move.Subscribe(v => moveDirection = v);
        }

        private void OnDisable()
        {
            m_Subscription?.Dispose();
        }
    }
}
