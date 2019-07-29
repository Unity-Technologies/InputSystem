// GENERATED AUTOMATICALLY FROM 'Assets/Demo/DemoControls.inputactions'

using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class DemoControls : IInputActionCollection
{
    private InputActionAsset asset;
    public DemoControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""DemoControls"",
    ""maps"": [
        {
            ""name"": ""gameplay"",
            ""id"": ""265c38f5-dd18-4d34-b198-aec58e1627ff"",
            ""actions"": [
                {
                    ""name"": ""fire"",
                    ""type"": ""Button"",
                    ""id"": ""1077f913-a9f9-41b1-acb3-b9ee0adbc744"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""move"",
                    ""type"": ""Value"",
                    ""id"": ""50fd2809-3aa3-4a90-988e-1facf6773553"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""look"",
                    ""type"": ""Value"",
                    ""id"": ""c60e0974-d140-4597-a40e-9862193067e9"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""menu"",
                    ""type"": ""Button"",
                    ""id"": ""4ad24240-1211-418c-9678-760c0f5e2f0f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""steamEnterMenu"",
                    ""type"": ""Button"",
                    ""id"": ""86bb1c77-7b7d-493c-94be-213881dd4b5b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""037e3e33-c574-4534-9ed9-adae48edd681"",
                    ""path"": ""*/{PrimaryAction}"",
                    ""interactions"": ""Tap,SlowTap"",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse;Gamepad"",
                    ""action"": ""fire"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ab73d0e0-aec1-4a81-b53b-e48c2c23a26b"",
                    ""path"": ""<SteamDemoController>/fire"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""fire"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8bc7a7b6-c643-43c9-bdcb-d5a07c4f2d89"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Dpad"",
                    ""id"": ""c3af592e-3e54-4cf5-b2f3-0a4f73edfda3"",
                    ""path"": ""Dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""955d37c0-3f4a-4b3e-acfa-bc6af46921e7"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""18cd010f-e2fb-423f-8b33-6116d85a190a"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""eb6362ed-0c2e-4c38-9b44-45a55226c9f3"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""526a8f1f-a9eb-4b75-b94c-1567ab349419"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""9ee5a9ec-e575-44bc-a9b4-89f53e8ee031"",
                    ""path"": ""<SteamDemoController>/move"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a453d940-8017-40fe-8aba-433cd6d230c6"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2527dafa-448f-4e8c-b19a-8a93f9b5e9b5"",
                    ""path"": ""<Pointer>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""36f9749e-bd8e-47f6-9d93-2c8676822ac0"",
                    ""path"": ""<SteamDemoController>/look"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b9d674c6-1b86-4a66-94b0-8b76815400a3"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""menu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1600ba4e-db08-48d2-b198-710c930cc4d9"",
                    ""path"": ""<Gamepad>/{Menu}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""menu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c1ace343-8981-4679-b66e-7a4e88b7beb0"",
                    ""path"": ""<SteamDemoController>/steamEnterMenu"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""steamEnterMenu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""menu"",
            ""id"": ""612b11fd-99c4-4a58-9c10-1d1b04fb8b30"",
            ""actions"": [
                {
                    ""name"": ""navigate"",
                    ""type"": ""Value"",
                    ""id"": ""21e4672f-7da8-41ac-8a80-59e98f44610f"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""click"",
                    ""type"": ""Button"",
                    ""id"": ""09cec56e-d919-461b-b769-b5f9040ab3d2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""steamExitMenu"",
                    ""type"": ""Button"",
                    ""id"": ""9493c6e9-a5fc-4534-8d0c-8730c350769d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""submit"",
                    ""type"": ""Button"",
                    ""id"": ""f6c0c6e8-e423-42cc-9071-06396825f4e2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""point"",
                    ""type"": ""Value"",
                    ""id"": ""2e1d701a-4b57-4fab-bee8-5ee788974fa6"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""9f9e76b2-108b-47a2-9321-e7a32e537b44"",
                    ""path"": ""<Gamepad>/{submit}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""submit"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""75489842-6ec7-4554-8c62-848d94099441"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""click"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""0079f24b-035c-4357-b8af-2f0d4661fc00"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""point"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c59c814e-6661-450c-8f56-cb77ac1bd8bd"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""navigate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7c7def0e-a802-4e7b-9287-b242eac5008d"",
                    ""path"": ""<Gamepad>/dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""navigate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard&Mouse"",
            ""basedOn"": """",
            ""bindingGroup"": ""KeyboardMouse"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""Gamepad"",
            ""basedOn"": """",
            ""bindingGroup"": ""Gamepad"",
            ""devices"": [
                {
                    ""devicePath"": ""<Gamepad>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""Steam"",
            ""basedOn"": """",
            ""bindingGroup"": ""Steam"",
            ""devices"": [
                {
                    ""devicePath"": ""<SteamDemoController>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""VR"",
            ""basedOn"": """",
            ""bindingGroup"": ""VR"",
            ""devices"": [
                {
                    ""devicePath"": ""<XRHMD>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<XRController>{LeftHand}"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<XRController>{RightHand}"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // gameplay
        m_gameplay = asset.GetActionMap("gameplay");
        m_gameplay_fire = m_gameplay.GetAction("fire");
        m_gameplay_move = m_gameplay.GetAction("move");
        m_gameplay_look = m_gameplay.GetAction("look");
        m_gameplay_menu = m_gameplay.GetAction("menu");
        m_gameplay_steamEnterMenu = m_gameplay.GetAction("steamEnterMenu");
        // menu
        m_menu = asset.GetActionMap("menu");
        m_menu_navigate = m_menu.GetAction("navigate");
        m_menu_click = m_menu.GetAction("click");
        m_menu_steamExitMenu = m_menu.GetAction("steamExitMenu");
        m_menu_submit = m_menu.GetAction("submit");
        m_menu_point = m_menu.GetAction("point");
    }

    ~DemoControls()
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
    private readonly InputAction m_gameplay_fire;
    private readonly InputAction m_gameplay_move;
    private readonly InputAction m_gameplay_look;
    private readonly InputAction m_gameplay_menu;
    private readonly InputAction m_gameplay_steamEnterMenu;
    public struct GameplayActions
    {
        private DemoControls m_Wrapper;
        public GameplayActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @fire => m_Wrapper.m_gameplay_fire;
        public InputAction @move => m_Wrapper.m_gameplay_move;
        public InputAction @look => m_Wrapper.m_gameplay_look;
        public InputAction @menu => m_Wrapper.m_gameplay_menu;
        public InputAction @steamEnterMenu => m_Wrapper.m_gameplay_steamEnterMenu;
        public InputActionMap Get() { return m_Wrapper.m_gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
        public void SetCallbacks(IGameplayActions instance)
        {
            if (m_Wrapper.m_GameplayActionsCallbackInterface != null)
            {
                fire.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnFire;
                fire.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnFire;
                fire.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnFire;
                move.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                move.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                move.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                look.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLook;
                look.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLook;
                look.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLook;
                menu.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMenu;
                menu.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMenu;
                menu.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMenu;
                steamEnterMenu.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnSteamEnterMenu;
                steamEnterMenu.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnSteamEnterMenu;
                steamEnterMenu.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnSteamEnterMenu;
            }
            m_Wrapper.m_GameplayActionsCallbackInterface = instance;
            if (instance != null)
            {
                fire.started += instance.OnFire;
                fire.performed += instance.OnFire;
                fire.canceled += instance.OnFire;
                move.started += instance.OnMove;
                move.performed += instance.OnMove;
                move.canceled += instance.OnMove;
                look.started += instance.OnLook;
                look.performed += instance.OnLook;
                look.canceled += instance.OnLook;
                menu.started += instance.OnMenu;
                menu.performed += instance.OnMenu;
                menu.canceled += instance.OnMenu;
                steamEnterMenu.started += instance.OnSteamEnterMenu;
                steamEnterMenu.performed += instance.OnSteamEnterMenu;
                steamEnterMenu.canceled += instance.OnSteamEnterMenu;
            }
        }
    }
    public GameplayActions @gameplay => new GameplayActions(this);

    // menu
    private readonly InputActionMap m_menu;
    private IMenuActions m_MenuActionsCallbackInterface;
    private readonly InputAction m_menu_navigate;
    private readonly InputAction m_menu_click;
    private readonly InputAction m_menu_steamExitMenu;
    private readonly InputAction m_menu_submit;
    private readonly InputAction m_menu_point;
    public struct MenuActions
    {
        private DemoControls m_Wrapper;
        public MenuActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @navigate => m_Wrapper.m_menu_navigate;
        public InputAction @click => m_Wrapper.m_menu_click;
        public InputAction @steamExitMenu => m_Wrapper.m_menu_steamExitMenu;
        public InputAction @submit => m_Wrapper.m_menu_submit;
        public InputAction @point => m_Wrapper.m_menu_point;
        public InputActionMap Get() { return m_Wrapper.m_menu; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(MenuActions set) { return set.Get(); }
        public void SetCallbacks(IMenuActions instance)
        {
            if (m_Wrapper.m_MenuActionsCallbackInterface != null)
            {
                navigate.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnNavigate;
                navigate.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnNavigate;
                navigate.canceled -= m_Wrapper.m_MenuActionsCallbackInterface.OnNavigate;
                click.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnClick;
                click.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnClick;
                click.canceled -= m_Wrapper.m_MenuActionsCallbackInterface.OnClick;
                steamExitMenu.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnSteamExitMenu;
                steamExitMenu.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnSteamExitMenu;
                steamExitMenu.canceled -= m_Wrapper.m_MenuActionsCallbackInterface.OnSteamExitMenu;
                submit.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnSubmit;
                submit.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnSubmit;
                submit.canceled -= m_Wrapper.m_MenuActionsCallbackInterface.OnSubmit;
                point.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnPoint;
                point.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnPoint;
                point.canceled -= m_Wrapper.m_MenuActionsCallbackInterface.OnPoint;
            }
            m_Wrapper.m_MenuActionsCallbackInterface = instance;
            if (instance != null)
            {
                navigate.started += instance.OnNavigate;
                navigate.performed += instance.OnNavigate;
                navigate.canceled += instance.OnNavigate;
                click.started += instance.OnClick;
                click.performed += instance.OnClick;
                click.canceled += instance.OnClick;
                steamExitMenu.started += instance.OnSteamExitMenu;
                steamExitMenu.performed += instance.OnSteamExitMenu;
                steamExitMenu.canceled += instance.OnSteamExitMenu;
                submit.started += instance.OnSubmit;
                submit.performed += instance.OnSubmit;
                submit.canceled += instance.OnSubmit;
                point.started += instance.OnPoint;
                point.performed += instance.OnPoint;
                point.canceled += instance.OnPoint;
            }
        }
    }
    public MenuActions @menu => new MenuActions(this);
    private int m_KeyboardMouseSchemeIndex = -1;
    public InputControlScheme KeyboardMouseScheme
    {
        get
        {
            if (m_KeyboardMouseSchemeIndex == -1) m_KeyboardMouseSchemeIndex = asset.GetControlSchemeIndex("Keyboard&Mouse");
            return asset.controlSchemes[m_KeyboardMouseSchemeIndex];
        }
    }
    private int m_GamepadSchemeIndex = -1;
    public InputControlScheme GamepadScheme
    {
        get
        {
            if (m_GamepadSchemeIndex == -1) m_GamepadSchemeIndex = asset.GetControlSchemeIndex("Gamepad");
            return asset.controlSchemes[m_GamepadSchemeIndex];
        }
    }
    private int m_SteamSchemeIndex = -1;
    public InputControlScheme SteamScheme
    {
        get
        {
            if (m_SteamSchemeIndex == -1) m_SteamSchemeIndex = asset.GetControlSchemeIndex("Steam");
            return asset.controlSchemes[m_SteamSchemeIndex];
        }
    }
    private int m_VRSchemeIndex = -1;
    public InputControlScheme VRScheme
    {
        get
        {
            if (m_VRSchemeIndex == -1) m_VRSchemeIndex = asset.GetControlSchemeIndex("VR");
            return asset.controlSchemes[m_VRSchemeIndex];
        }
    }
    public interface IGameplayActions
    {
        void OnFire(InputAction.CallbackContext context);
        void OnMove(InputAction.CallbackContext context);
        void OnLook(InputAction.CallbackContext context);
        void OnMenu(InputAction.CallbackContext context);
        void OnSteamEnterMenu(InputAction.CallbackContext context);
    }
    public interface IMenuActions
    {
        void OnNavigate(InputAction.CallbackContext context);
        void OnClick(InputAction.CallbackContext context);
        void OnSteamExitMenu(InputAction.CallbackContext context);
        void OnSubmit(InputAction.CallbackContext context);
        void OnPoint(InputAction.CallbackContext context);
    }
}
