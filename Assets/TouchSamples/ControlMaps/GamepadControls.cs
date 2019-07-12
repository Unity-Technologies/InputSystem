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
                    ""type"": ""Value"",
                    ""id"": ""dc21bb32-67fb-48db-9646-5dd93115cdf2"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""button1Action"",
                    ""type"": ""Value"",
                    ""id"": ""2b9eb09b-8a1c-443e-8c7a-16b3cea62dcf"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""button2Action"",
                    ""type"": ""Value"",
                    ""id"": ""69948a22-635b-456f-8308-9700d9c9c1d1"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""82c86c71-d077-46d8-9d79-1e9ee65186f2"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Dpad"",
                    ""id"": ""2c04063e-f310-4621-9239-18ca1160a3e1"",
                    ""path"": ""Dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""183faac1-0217-4cad-861b-e7067c90a028"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""6fbfef2b-ed01-4b1d-ae06-588329e44ad4"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""d830f1c5-6105-45dc-b815-2972c9a69a41"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""3f502839-3577-46b0-a41c-ad96b9434a24"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""02cdda47-2975-4699-a63a-8bd6086b7d88"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button1Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8f6891a1-22ca-4118-bc17-2a94ce0b4582"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button1Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d917f46d-6e17-43ad-8cc3-aa84a67cd298"",
                    ""path"": ""<Keyboard>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button1Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""bee26bd4-a054-43b7-b891-e99402a39f29"",
                    ""path"": ""<Gamepad>/buttonEast"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button2Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7eed8180-027d-46fe-a6ec-0a678bbecb11"",
                    ""path"": ""<Keyboard>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""button2Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
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

        // gameplay
        private readonly InputActionMap m_gameplay;
        private IGameplayActions m_GameplayActionsCallbackInterface;
        private readonly InputAction m_gameplay_movement;
        private readonly InputAction m_gameplay_button1Action;
        private readonly InputAction m_gameplay_button2Action;
        public struct GameplayActions
        {
            private GamepadControls m_Wrapper;
            public GameplayActions(GamepadControls wrapper) { m_Wrapper = wrapper; }
            public InputAction @movement => m_Wrapper.m_gameplay_movement;
            public InputAction @button1Action => m_Wrapper.m_gameplay_button1Action;
            public InputAction @button2Action => m_Wrapper.m_gameplay_button2Action;
            public InputActionMap Get() { return m_Wrapper.m_gameplay; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
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
        public GameplayActions @gameplay => new GameplayActions(this);
        public interface IGameplayActions
        {
            void OnMovement(InputAction.CallbackContext context);
            void OnButton1Action(InputAction.CallbackContext context);
            void OnButton2Action(InputAction.CallbackContext context);
        }
    }
}
