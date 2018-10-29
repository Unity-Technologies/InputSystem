// GENERATED AUTOMATICALLY FROM 'Assets/Demo/DemoControls.inputactions'

using System;
using UnityEngine;
using UnityEngine.Experimental.Input;


[Serializable]
public class DemoControls : InputActionAssetReference
{
    public DemoControls()
    {
    }

    public DemoControls(InputActionAsset asset)
        : base(asset)
    {
    }

    private bool m_Initialized;
    private void Initialize()
    {
        // gameplay
        m_gameplay = asset.GetActionMap("gameplay");
        m_gameplay_fire = m_gameplay.GetAction("fire");
        m_gameplay_move = m_gameplay.GetAction("move");
        m_gameplay_look = m_gameplay.GetAction("look");
        m_gameplay_jump = m_gameplay.GetAction("jump");
        m_gameplay_escape = m_gameplay.GetAction("escape");
        // menu
        m_menu = asset.GetActionMap("menu");
        m_menu_navigate = m_menu.GetAction("navigate");
        m_menu_click = m_menu.GetAction("click");
        m_Initialized = true;
    }

    private void Uninitialize()
    {
        m_gameplay = null;
        m_gameplay_fire = null;
        m_gameplay_move = null;
        m_gameplay_look = null;
        m_gameplay_jump = null;
        m_gameplay_escape = null;
        m_menu = null;
        m_menu_navigate = null;
        m_menu_click = null;
        m_Initialized = false;
    }

    public void SwitchAsset(InputActionAsset newAsset)
    {
        if (newAsset == asset) return;
        if (m_Initialized) Uninitialize();
        asset = newAsset;
    }

    public void DuplicateAndSwitchAsset()
    {
        SwitchAsset(ScriptableObject.Instantiate(asset));
    }

    // gameplay
    private InputActionMap m_gameplay;
    private IGameplayActions m_GameplayActionsCallbackInterface;
    private InputAction m_gameplay_fire;
    private InputAction m_gameplay_move;
    private InputAction m_gameplay_look;
    private InputAction m_gameplay_jump;
    private InputAction m_gameplay_escape;
    public struct GameplayActions
    {
        private DemoControls m_Wrapper;
        public GameplayActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @fire { get { return m_Wrapper.m_gameplay_fire; } }
        public InputAction @move { get { return m_Wrapper.m_gameplay_move; } }
        public InputAction @look { get { return m_Wrapper.m_gameplay_look; } }
        public InputAction @jump { get { return m_Wrapper.m_gameplay_jump; } }
        public InputAction @escape { get { return m_Wrapper.m_gameplay_escape; } }
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
                jump.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnJump;
                jump.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnJump;
                jump.cancelled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnJump;
                escape.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnEscape;
                escape.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnEscape;
                escape.cancelled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnEscape;
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
                jump.started += instance.OnJump;
                jump.performed += instance.OnJump;
                jump.cancelled += instance.OnJump;
                escape.started += instance.OnEscape;
                escape.performed += instance.OnEscape;
                escape.cancelled += instance.OnEscape;
            }
        }
    }
    public GameplayActions @gameplay
    {
        get
        {
            if (!m_Initialized) Initialize();
            return new GameplayActions(this);
        }
    }
    // menu
    private InputActionMap m_menu;
    private IMenuActions m_MenuActionsCallbackInterface;
    private InputAction m_menu_navigate;
    private InputAction m_menu_click;
    public struct MenuActions
    {
        private DemoControls m_Wrapper;
        public MenuActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @navigate { get { return m_Wrapper.m_menu_navigate; } }
        public InputAction @click { get { return m_Wrapper.m_menu_click; } }
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
            }
        }
    }
    public MenuActions @menu
    {
        get
        {
            if (!m_Initialized) Initialize();
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
}
public interface IGameplayActions
{
    void OnFire(InputAction.CallbackContext context);
    void OnMove(InputAction.CallbackContext context);
    void OnLook(InputAction.CallbackContext context);
    void OnJump(InputAction.CallbackContext context);
    void OnEscape(InputAction.CallbackContext context);
}
public interface IMenuActions
{
    void OnNavigate(InputAction.CallbackContext context);
    void OnClick(InputAction.CallbackContext context);
}
