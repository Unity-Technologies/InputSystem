// GENERATED AUTOMATICALLY FROM 'Assets/TouchSamples/ControlMaps/GamepadControls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
                    ""id"": ""00c91ea0-ac4a-4ceb-80c6-eed25a87f19f"",
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
                    ""id"": ""1d11b9e7-bfb1-419b-abb6-a516a72a556b"",
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
                    ""id"": ""86a77f0f-8998-4fad-8be1-67eb49eca24a"",
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
                    ""id"": ""f8cd4a80-de8a-4102-89ee-f483c5cc6406"",
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
                    ""id"": ""8de14253-2102-40a1-a1e2-1e7e3686ebbc"",
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
                    ""id"": ""391076d3-fb41-4537-ad47-760d796077ab"",
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
                    ""id"": ""1c751639-1499-4735-af5d-5a1e9795d634"",
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
                    ""id"": ""d4548d25-4599-4f84-b061-4f9ced906a8b"",
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
                    ""id"": ""fb724a2d-3927-41ab-8662-8280837c4cb8"",
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
                    ""id"": ""f7b79851-1e84-4d66-b933-68948464a11b"",
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
                    ""id"": ""6f95a181-cdc2-4787-bf85-1818c20bbe0e"",
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
