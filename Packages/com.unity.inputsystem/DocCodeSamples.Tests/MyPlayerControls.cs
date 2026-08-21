// This file mimics the C# class that Unity's Input Action Code Generator
// would produce for an .inputactions asset with a "gameplay" action map
// containing "use" and "move" actions. It exists purely so that
// GenerateCsApiFromActions.cs has a real MyPlayerControls/IGameplayActions
// pair to compile against for the "Generate C# Class" documentation sample.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace DocCodeSamples.Tests
{
    public partial class MyPlayerControls : IInputActionCollection2, IDisposable
    {
        public InputActionAsset asset { get; }

        public MyPlayerControls()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""MyPlayerControls"",
    ""maps"": [
        {
            ""name"": ""gameplay"",
            ""id"": ""d55be63c-61eb-47ef-92dd-eef1248d601e"",
            ""actions"": [
                {
                    ""name"": ""move"",
                    ""type"": ""Value"",
                    ""id"": ""8387a17d-aedd-4411-9931-6a855a8299fb"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""use"",
                    ""type"": ""Button"",
                    ""id"": ""b5f08480-c03b-4654-8475-9c94e8ccccaf"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""2c541328-ed00-4524-817c-97599bac7de5"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""41041dd1-570a-487c-856e-d58cfa06509a"",
                    ""path"": ""<Gamepad>/buttonNorth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""use"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
            // gameplay
            m_gameplay = asset.FindActionMap("gameplay", throwIfNotFound: true);
            m_gameplay_move = m_gameplay.FindAction("move", throwIfNotFound: true);
            m_gameplay_use = m_gameplay.FindAction("use", throwIfNotFound: true);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Enable()
        {
            asset.Enable();
        }

        public void Disable()
        {
            asset.Disable();
        }

        public IEnumerable<InputBinding> bindings => asset.bindings;

        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            return asset.FindAction(actionNameOrId, throwIfNotFound);
        }

        public int FindBinding(InputBinding bindingMask, out InputAction action)
        {
            return asset.FindBinding(bindingMask, out action);
        }

        // gameplay
        private readonly InputActionMap m_gameplay;
        private IGameplayActions m_GameplayActionsCallbackInterface;
        private readonly InputAction m_gameplay_move;
        private readonly InputAction m_gameplay_use;
        public struct GameplayActions
        {
            private MyPlayerControls m_Wrapper;
            public GameplayActions(MyPlayerControls wrapper) { m_Wrapper = wrapper; }
            public InputAction @move => m_Wrapper.m_gameplay_move;
            public InputAction @use => m_Wrapper.m_gameplay_use;
            public InputActionMap Get() { return m_Wrapper.m_gameplay; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
            public void SetCallbacks(IGameplayActions instance)
            {
                if (m_Wrapper.m_GameplayActionsCallbackInterface != null)
                {
                    @move.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                    @move.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                    @move.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                    @use.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnUse;
                    @use.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnUse;
                    @use.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnUse;
                }
                m_Wrapper.m_GameplayActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @move.started += instance.OnMove;
                    @move.performed += instance.OnMove;
                    @move.canceled += instance.OnMove;
                    @use.started += instance.OnUse;
                    @use.performed += instance.OnUse;
                    @use.canceled += instance.OnUse;
                }
            }
        }
        public GameplayActions @gameplay => new GameplayActions(this);

        public interface IGameplayActions
        {
            void OnMove(InputAction.CallbackContext context);
            void OnUse(InputAction.CallbackContext context);
        }
    }
}
