// GENERATED AUTOMATICALLY FROM 'Assets/Demo/DemoControls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Utilities;


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
                    ""id"": ""1077f913-a9f9-41b1-acb3-b9ee0adbc744"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""move"",
                    ""id"": ""50fd2809-3aa3-4a90-988e-1facf6773553"",
                    ""expectedControlLayout"": ""Vector2"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""look"",
                    ""id"": ""c60e0974-d140-4597-a40e-9862193067e9"",
                    ""expectedControlLayout"": ""Vector2"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""menu"",
                    ""id"": ""4ad24240-1211-418c-9678-760c0f5e2f0f"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""steamEnterMenu"",
                    ""id"": ""86bb1c77-7b7d-493c-94be-213881dd4b5b"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""835e9892-47f3-46d8-ab24-2da6a2366b12"",
                    ""path"": ""*/{PrimaryAction}"",
                    ""interactions"": ""Tap,SlowTap"",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse;Gamepad"",
                    ""action"": ""fire"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""afc296ad-2abf-49e7-824d-01b1eaedd561"",
                    ""path"": ""<SteamDemoController>/fire"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""fire"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""f790a8f2-bc8b-4101-a4f1-d5dbe38d734b"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""move"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""Dpad"",
                    ""id"": ""f0f22493-fa97-4137-b13f-eff19c3611d1"",
                    ""path"": ""Dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": true,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""up"",
                    ""id"": ""2298cc23-334c-4e16-a245-05cfa7759401"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""down"",
                    ""id"": ""6db34b29-292c-4a29-b4cb-a0da831edec8"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""left"",
                    ""id"": ""0859e0ab-899d-4a28-8793-25971e0071bc"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""right"",
                    ""id"": ""44ba2502-7ad7-411d-9cf1-93b7fb32bf6e"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""96e8270e-7c43-462d-828c-1725d8b29a60"",
                    ""path"": ""<SteamDemoController>/move"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""move"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""c923feb9-55e4-49c5-a7ff-2ae0da008a10"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""look"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""5be963c0-ebc4-438c-a6e9-aa49ec9949b0"",
                    ""path"": ""<Pointer>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""look"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""91bb5928-80b6-420c-9749-2148d8187723"",
                    ""path"": ""<SteamDemoController>/look"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""look"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""cd8bd088-9dab-41ef-9e07-ba6d8050f977"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""menu"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""4d11a9d7-8132-4b9a-b56b-1762eac051e3"",
                    ""path"": ""<Gamepad>/{Menu}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""menu"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""3a9e1481-5089-4ab2-bf4b-7fa120cc354e"",
                    ""path"": ""<SteamDemoController>/steamEnterMenu"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""steamEnterMenu"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                }
            ]
        },
        {
            ""name"": ""menu"",
            ""id"": ""612b11fd-99c4-4a58-9c10-1d1b04fb8b30"",
            ""actions"": [
                {
                    ""name"": ""navigate"",
                    ""id"": ""21e4672f-7da8-41ac-8a80-59e98f44610f"",
                    ""expectedControlLayout"": ""Vector2"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""click"",
                    ""id"": ""09cec56e-d919-461b-b769-b5f9040ab3d2"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""steamExitMenu"",
                    ""id"": ""9493c6e9-a5fc-4534-8d0c-8730c350769d"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""submit"",
                    ""id"": ""f6c0c6e8-e423-42cc-9071-06396825f4e2"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""point"",
                    ""id"": ""2e1d701a-4b57-4fab-bee8-5ee788974fa6"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""3e2c3290-80ea-44d4-85bb-6f0af361d6c9"",
                    ""path"": ""<Gamepad>/{submit}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""submit"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""2d538a69-63a4-41b6-996c-ded14de15d15"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""click"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""d0a0cf31-41c8-45f0-ad20-508d6d485d77"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""point"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""ecc5f077-2e3d-4ad9-86e0-767d520c9181"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""navigate"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""6356f6e3-5221-4d20-9b4e-463b84f1b3ba"",
                    ""path"": ""<Gamepad>/dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""navigate"",
                    ""chainWithPrevious"": false,
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
    private InputAction m_gameplay_fire;
    private InputAction m_gameplay_move;
    private InputAction m_gameplay_look;
    private InputAction m_gameplay_menu;
    private InputAction m_gameplay_steamEnterMenu;
    public struct GameplayActions
    {
        private DemoControls m_Wrapper;
        public GameplayActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @fire { get { return m_Wrapper.m_gameplay_fire; } }
        public InputAction @move { get { return m_Wrapper.m_gameplay_move; } }
        public InputAction @look { get { return m_Wrapper.m_gameplay_look; } }
        public InputAction @menu { get { return m_Wrapper.m_gameplay_menu; } }
        public InputAction @steamEnterMenu { get { return m_Wrapper.m_gameplay_steamEnterMenu; } }
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
                fire.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnFire;
                fire.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnFire;
                fire.cancelled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnFire;
                move.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                move.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                move.cancelled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                look.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLook;
                look.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLook;
                look.cancelled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLook;
                menu.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMenu;
                menu.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMenu;
                menu.cancelled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMenu;
                steamEnterMenu.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnSteamEnterMenu;
                steamEnterMenu.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnSteamEnterMenu;
                steamEnterMenu.cancelled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnSteamEnterMenu;
            }
            m_Wrapper.m_GameplayActionsCallbackInterface = instance;
            if (instance != null)
            {
                fire.started += instance.OnFire;
                fire.performed += instance.OnFire;
                fire.cancelled += instance.OnFire;
                move.started += instance.OnMove;
                move.performed += instance.OnMove;
                move.cancelled += instance.OnMove;
                look.started += instance.OnLook;
                look.performed += instance.OnLook;
                look.cancelled += instance.OnLook;
                menu.started += instance.OnMenu;
                menu.performed += instance.OnMenu;
                menu.cancelled += instance.OnMenu;
                steamEnterMenu.started += instance.OnSteamEnterMenu;
                steamEnterMenu.performed += instance.OnSteamEnterMenu;
                steamEnterMenu.cancelled += instance.OnSteamEnterMenu;
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
    // menu
    private InputActionMap m_menu;
    private IMenuActions m_MenuActionsCallbackInterface;
    private InputAction m_menu_navigate;
    private InputAction m_menu_click;
    private InputAction m_menu_steamExitMenu;
    private InputAction m_menu_submit;
    private InputAction m_menu_point;
    public struct MenuActions
    {
        private DemoControls m_Wrapper;
        public MenuActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @navigate { get { return m_Wrapper.m_menu_navigate; } }
        public InputAction @click { get { return m_Wrapper.m_menu_click; } }
        public InputAction @steamExitMenu { get { return m_Wrapper.m_menu_steamExitMenu; } }
        public InputAction @submit { get { return m_Wrapper.m_menu_submit; } }
        public InputAction @point { get { return m_Wrapper.m_menu_point; } }
        public InputActionMap Get() { return m_Wrapper.m_menu; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled { get { return Get().enabled; } }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(MenuActions set) { return set.Get(); }
        public void SetCallbacks(IMenuActions instance)
        {
            if (m_Wrapper.m_MenuActionsCallbackInterface != null)
            {
                navigate.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnNavigate;
                navigate.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnNavigate;
                navigate.cancelled -= m_Wrapper.m_MenuActionsCallbackInterface.OnNavigate;
                click.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnClick;
                click.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnClick;
                click.cancelled -= m_Wrapper.m_MenuActionsCallbackInterface.OnClick;
                steamExitMenu.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnSteamExitMenu;
                steamExitMenu.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnSteamExitMenu;
                steamExitMenu.cancelled -= m_Wrapper.m_MenuActionsCallbackInterface.OnSteamExitMenu;
                submit.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnSubmit;
                submit.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnSubmit;
                submit.cancelled -= m_Wrapper.m_MenuActionsCallbackInterface.OnSubmit;
                point.started -= m_Wrapper.m_MenuActionsCallbackInterface.OnPoint;
                point.performed -= m_Wrapper.m_MenuActionsCallbackInterface.OnPoint;
                point.cancelled -= m_Wrapper.m_MenuActionsCallbackInterface.OnPoint;
            }
            m_Wrapper.m_MenuActionsCallbackInterface = instance;
            if (instance != null)
            {
                navigate.started += instance.OnNavigate;
                navigate.performed += instance.OnNavigate;
                navigate.cancelled += instance.OnNavigate;
                click.started += instance.OnClick;
                click.performed += instance.OnClick;
                click.cancelled += instance.OnClick;
                steamExitMenu.started += instance.OnSteamExitMenu;
                steamExitMenu.performed += instance.OnSteamExitMenu;
                steamExitMenu.cancelled += instance.OnSteamExitMenu;
                submit.started += instance.OnSubmit;
                submit.performed += instance.OnSubmit;
                submit.cancelled += instance.OnSubmit;
                point.started += instance.OnPoint;
                point.performed += instance.OnPoint;
                point.cancelled += instance.OnPoint;
            }
        }
    }
    public MenuActions @menu
    {
        get
        {
            return new MenuActions(this);
        }
    }
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
