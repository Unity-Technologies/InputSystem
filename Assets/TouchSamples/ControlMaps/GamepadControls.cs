// GENERATED AUTOMATICALLY FROM 'Assets/TouchSamples/ControlMaps/GamepadControls.inputactions'

using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace InputSamples.Controls
{
    public class GamepadControls : IInputActionCollection
    {
        private InputActionAsset asset;
        public GamepadControls()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""GamepadControls"",
    ""maps"": [
        {
            ""name"": ""gameplay"",
            ""id"": ""d037cf2c-5453-4c50-8506-b66efdbbf3e1"",
            ""actions"": [
                {
                    ""name"": ""movement"",
                    ""id"": ""dc21bb32-67fb-48db-9646-5dd93115cdf2"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""button1Action"",
                    ""id"": ""2b9eb09b-8a1c-443e-8c7a-16b3cea62dcf"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""button2Action"",
                    ""id"": ""69948a22-635b-456f-8308-9700d9c9c1d1"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""8ec6e021-8b54-411b-9627-ba3a1e933086"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""Dpad"",
                    ""id"": ""b50ec9e9-e0bb-483f-8bb3-532c297eb91a"",
                    ""path"": ""Dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""up"",
                    ""id"": ""d6c9c892-f7c7-4b66-8538-1d54cec881c6"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""down"",
                    ""id"": ""09fa0e5f-a12f-4b89-9f2f-5d93bc8e75d9"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""left"",
                    ""id"": ""8f5daab8-c0e1-4c20-9a26-c303cf34d079"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""right"",
                    ""id"": ""deb1e07d-8d1e-44b7-b77d-bb6921322afd"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""4cd902ea-3e98-4702-a5f1-077ec0894ee1"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button1Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""ce49b162-6ccc-4c5f-9269-37597027bd67"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button1Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""dbc381e2-2cbc-45da-a3ae-5a53e75d33e2"",
                    ""path"": ""<Keyboard>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button1Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""27fadae3-cdc9-4ce0-bac8-680a990a145a"",
                    ""path"": ""<Gamepad>/buttonEast"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button2Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""5610b284-46a3-4796-83ef-61e6a5afdef8"",
                    ""path"": ""<Keyboard>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button2Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
            // gameplay
            m_gameplay = asset.GetActionMap("gameplay");
            m_gameplay_movement = m_gameplay.GetAction("movement");
            m_gameplay_button1Action = m_gameplay.GetAction("button1Action");
            m_gameplay_button2Action = m_gameplay.GetAction("button2Action");
        }

        ~GamepadControls()
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

        public ReadOnlyArray<InputControlScheme> controlSchemes
        {
            get => asset.controlSchemes;
        }

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

        // gameplay
        private InputActionMap m_gameplay;
        private IGameplayActions m_GameplayActionsCallbackInterface;
        private InputAction m_gameplay_movement;
        private InputAction m_gameplay_button1Action;
        private InputAction m_gameplay_button2Action;
        public struct GameplayActions
        {
            private GamepadControls m_Wrapper;
            public GameplayActions(GamepadControls wrapper) { m_Wrapper = wrapper; }
            public InputAction @movement { get { return m_Wrapper.m_gameplay_movement; } }
            public InputAction @button1Action { get { return m_Wrapper.m_gameplay_button1Action; } }
            public InputAction @button2Action { get { return m_Wrapper.m_gameplay_button2Action; } }
            public InputActionMap Get() { return m_Wrapper.m_gameplay; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled { get { return Get().enabled; } }
            public InputActionMap Clone() { return Get().Clone(); }
            public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
            public void SetCallbacks(IGameplayActions instance)
            {
                if (m_Wrapper.m_GameplayActionsCallbackInterface != null)
                {
                    movement.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMovement;
                    movement.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMovement;
                    movement.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMovement;
                    button1Action.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnButton1Action;
                    button1Action.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnButton1Action;
                    button1Action.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnButton1Action;
                    button2Action.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnButton2Action;
                    button2Action.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnButton2Action;
                    button2Action.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnButton2Action;
                }
                m_Wrapper.m_GameplayActionsCallbackInterface = instance;
                if (instance != null)
                {
                    movement.started += instance.OnMovement;
                    movement.performed += instance.OnMovement;
                    movement.canceled += instance.OnMovement;
                    button1Action.started += instance.OnButton1Action;
                    button1Action.performed += instance.OnButton1Action;
                    button1Action.canceled += instance.OnButton1Action;
                    button2Action.started += instance.OnButton2Action;
                    button2Action.performed += instance.OnButton2Action;
                    button2Action.canceled += instance.OnButton2Action;
                }
            }
        }
        public GameplayActions @gameplay
        {
            get
            {
                return new GameplayActions(this);
            }
        }
        public interface IGameplayActions
        {
            void OnMovement(InputAction.CallbackContext context);
            void OnButton1Action(InputAction.CallbackContext context);
            void OnButton2Action(InputAction.CallbackContext context);
        }
    }
}
