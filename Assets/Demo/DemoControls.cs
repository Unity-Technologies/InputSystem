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
                    ""initialStateCheck"": false,
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
                    ""initialStateCheck"": false,
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
                    ""initialStateCheck"": false,
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
                    ""initialStateCheck"": false,
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
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""32903807-5ff8-450d-84b8-28cf7be6693e"",
                    ""path"": ""*/{PrimaryAction}"",
                    ""interactions"": ""Tap,SlowTap"",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse;Gamepad"",
                    ""action"": ""fire"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""8fba42e9-eb84-47e4-a75d-2f033a4729be"",
                    ""path"": ""<SteamDemoController>/fire"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""fire"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""a10fdc05-19c6-4cfe-9da3-7d18581e149e"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""Dpad"",
                    ""id"": ""ab7b6883-7a24-4107-b6c4-5699f27e6bd3"",
                    ""path"": ""Dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""up"",
                    ""id"": ""d336c06b-1f43-4239-af1d-c3fc2d85f6c5"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""down"",
                    ""id"": ""23d439bf-a75f-4264-ab19-983e27ebbc3a"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""left"",
                    ""id"": ""21361150-ed3d-44c2-af1d-1ed1558a70a7"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""right"",
                    ""id"": ""c4838935-907b-41c9-93a5-aba093da9975"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""aff3dca4-08a4-4548-ad13-ecd47573b6f9"",
                    ""path"": ""<SteamDemoController>/move"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""9c8ab964-ba94-488d-ae29-9aaa306c44aa"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""1e567886-e4a0-4513-9027-552c8c677125"",
                    ""path"": ""<Pointer>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""fca69726-12c7-4d66-8ed5-7c96b5cbca4c"",
                    ""path"": ""<SteamDemoController>/look"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""d07af1bb-9562-4cbe-80ae-1681d9c8876a"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""action"": ""menu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""eee60263-6103-4168-9630-f93592d6e9f6"",
                    ""path"": ""<Gamepad>/{Menu}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""menu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""ec9f0524-23c1-4c7f-82e7-4bb22b7c7563"",
                    ""path"": ""<SteamDemoController>/steamEnterMenu"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Steam"",
                    ""action"": ""steamEnterMenu"",
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
                    ""initialStateCheck"": false,
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
                    ""initialStateCheck"": false,
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
                    ""initialStateCheck"": false,
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
                    ""initialStateCheck"": false,
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
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""6e0bea17-335c-47c6-a8d0-24fe379d3838"",
                    ""path"": ""<Gamepad>/{submit}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""submit"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""aa3053fd-b0e3-48b8-a058-d0302975f26f"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""click"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""36c8e210-47ea-4871-8b51-d1137bfc52b4"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""point"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""86c9f93b-f037-4748-b844-5bc9904ed143"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""navigate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""6f06775c-1269-4fe2-a8da-997f015d9b0a"",
                    ""path"": ""<Gamepad>/dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""navigate"",
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
