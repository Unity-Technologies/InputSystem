using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.Serialization;
using Gamepad = UnityEngine.InputSystem.Experimental.Devices.Gamepad;
using Keyboard = UnityEngine.InputSystem.Experimental.Devices.Keyboard;
using Vector2 = UnityEngine.Vector2;

namespace UseCases
{
    // TODO We might actually use an importer and when we import we will create an InputBindingAsset that can create the represented object for us.
    //      Why wouldn't we parse the configuration already at editor stage? Then there is no work to do during import.
    
    // Strategy pattern: https://unity.com/how-to/scriptableobjects-delegate-objects
    
    // Scripted importer: https://medium.com/miijiis-unified-works/have-unity-support-your-custom-file-part-4-6-fc2ae4ec09c0

    
    
    // TODO Use customer picker with a tab containing presets?
    
    // TODO Implement a ScriptableInputBindingObject that wraps an .inputactions asset? 
    
    /*[CreateAssetMenu(menuName = "Input/Input Binding", fileName = "Move")]
    public class MovePreset : ScriptableInputBinding<Vector2>
    {
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer)
        {
            // TODO Basically this is where we should create an internal mux and just aggregate?!
            return Combine.Composite(Keyboard.A, Keyboard.D, Keyboard.S, Keyboard.W)
                .Subscribe(context, observer);
        }
    }*/
    
    
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

        [Tooltip("Allows configuring an input binding for moving")]
        public ScriptableInputBinding<Vector2> move;

        [Tooltip("Allows configuring an input binding for jumping")]
        public ScriptableInputBinding<InputEvent> jump;

        //public BindableInput<Vector2> m_Move = new BindableInput<Vector2>(); // TODO Should be a scriptable object or extracted from one instead of new

        //public BindableInput<InputEvent> m_Celebrate;
        
        private IDisposable m_Subscription, m_JumpSubscription;
    
        private void OnEnable()
        {
            // Note: Using TrySubscribe instead of Subscribe which doesn't fail if "move" is null.
            m_Subscription = move.TrySubscribe(v => moveDirection = v);

            m_JumpSubscription = jump.TrySubscribe(v => Debug.Log("Jump"));
        }

        private void OnDisable()
        {
            m_Subscription?.Dispose();
            m_JumpSubscription?.Dispose();
        }
        
        /*
        var types = TypeCache.GetTypesDerivedFrom<ScriptableInputBinding>();
            foreach (var type in types)
        {
            if (type.IsAbstract)
                continue;
            Debug.Log("Types: " + type);
        }*/
    }
}
