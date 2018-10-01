// GENERATED AUTOMATICALLY FROM 'Assets/Demo/DemoControls.inputactions'

using System;
using UnityEngine;
using UnityEngine.Events;
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
        if (m_gameplayFireActionStarted != null)
            m_gameplay_fire.started += m_gameplayFireActionStarted.Invoke;
        if (m_gameplayFireActionPerformed != null)
            m_gameplay_fire.performed += m_gameplayFireActionPerformed.Invoke;
        if (m_gameplayFireActionCancelled != null)
            m_gameplay_fire.cancelled += m_gameplayFireActionCancelled.Invoke;
        m_gameplay_move = m_gameplay.GetAction("move");
        if (m_gameplayMoveActionStarted != null)
            m_gameplay_move.started += m_gameplayMoveActionStarted.Invoke;
        if (m_gameplayMoveActionPerformed != null)
            m_gameplay_move.performed += m_gameplayMoveActionPerformed.Invoke;
        if (m_gameplayMoveActionCancelled != null)
            m_gameplay_move.cancelled += m_gameplayMoveActionCancelled.Invoke;
        m_gameplay_look = m_gameplay.GetAction("look");
        if (m_gameplayLookActionStarted != null)
            m_gameplay_look.started += m_gameplayLookActionStarted.Invoke;
        if (m_gameplayLookActionPerformed != null)
            m_gameplay_look.performed += m_gameplayLookActionPerformed.Invoke;
        if (m_gameplayLookActionCancelled != null)
            m_gameplay_look.cancelled += m_gameplayLookActionCancelled.Invoke;
        m_gameplay_jump = m_gameplay.GetAction("jump");
        if (m_gameplayJumpActionStarted != null)
            m_gameplay_jump.started += m_gameplayJumpActionStarted.Invoke;
        if (m_gameplayJumpActionPerformed != null)
            m_gameplay_jump.performed += m_gameplayJumpActionPerformed.Invoke;
        if (m_gameplayJumpActionCancelled != null)
            m_gameplay_jump.cancelled += m_gameplayJumpActionCancelled.Invoke;
        m_gameplay_escape = m_gameplay.GetAction("escape");
        if (m_gameplayEscapeActionStarted != null)
            m_gameplay_escape.started += m_gameplayEscapeActionStarted.Invoke;
        if (m_gameplayEscapeActionPerformed != null)
            m_gameplay_escape.performed += m_gameplayEscapeActionPerformed.Invoke;
        if (m_gameplayEscapeActionCancelled != null)
            m_gameplay_escape.cancelled += m_gameplayEscapeActionCancelled.Invoke;
        // menu
        m_menu = asset.GetActionMap("menu");
        m_menu_navigate = m_menu.GetAction("navigate");
        if (m_menuNavigateActionStarted != null)
            m_menu_navigate.started += m_menuNavigateActionStarted.Invoke;
        if (m_menuNavigateActionPerformed != null)
            m_menu_navigate.performed += m_menuNavigateActionPerformed.Invoke;
        if (m_menuNavigateActionCancelled != null)
            m_menu_navigate.cancelled += m_menuNavigateActionCancelled.Invoke;
        m_menu_click = m_menu.GetAction("click");
        if (m_menuClickActionStarted != null)
            m_menu_click.started += m_menuClickActionStarted.Invoke;
        if (m_menuClickActionPerformed != null)
            m_menu_click.performed += m_menuClickActionPerformed.Invoke;
        if (m_menuClickActionCancelled != null)
            m_menu_click.cancelled += m_menuClickActionCancelled.Invoke;
        m_Initialized = true;
    }

    private void Uninitialize()
    {
        m_gameplay = null;
        m_gameplay_fire = null;
        if (m_gameplayFireActionStarted != null)
            m_gameplay_fire.started -= m_gameplayFireActionStarted.Invoke;
        if (m_gameplayFireActionPerformed != null)
            m_gameplay_fire.performed -= m_gameplayFireActionPerformed.Invoke;
        if (m_gameplayFireActionCancelled != null)
            m_gameplay_fire.cancelled -= m_gameplayFireActionCancelled.Invoke;
        m_gameplay_move = null;
        if (m_gameplayMoveActionStarted != null)
            m_gameplay_move.started -= m_gameplayMoveActionStarted.Invoke;
        if (m_gameplayMoveActionPerformed != null)
            m_gameplay_move.performed -= m_gameplayMoveActionPerformed.Invoke;
        if (m_gameplayMoveActionCancelled != null)
            m_gameplay_move.cancelled -= m_gameplayMoveActionCancelled.Invoke;
        m_gameplay_look = null;
        if (m_gameplayLookActionStarted != null)
            m_gameplay_look.started -= m_gameplayLookActionStarted.Invoke;
        if (m_gameplayLookActionPerformed != null)
            m_gameplay_look.performed -= m_gameplayLookActionPerformed.Invoke;
        if (m_gameplayLookActionCancelled != null)
            m_gameplay_look.cancelled -= m_gameplayLookActionCancelled.Invoke;
        m_gameplay_jump = null;
        if (m_gameplayJumpActionStarted != null)
            m_gameplay_jump.started -= m_gameplayJumpActionStarted.Invoke;
        if (m_gameplayJumpActionPerformed != null)
            m_gameplay_jump.performed -= m_gameplayJumpActionPerformed.Invoke;
        if (m_gameplayJumpActionCancelled != null)
            m_gameplay_jump.cancelled -= m_gameplayJumpActionCancelled.Invoke;
        m_gameplay_escape = null;
        if (m_gameplayEscapeActionStarted != null)
            m_gameplay_escape.started -= m_gameplayEscapeActionStarted.Invoke;
        if (m_gameplayEscapeActionPerformed != null)
            m_gameplay_escape.performed -= m_gameplayEscapeActionPerformed.Invoke;
        if (m_gameplayEscapeActionCancelled != null)
            m_gameplay_escape.cancelled -= m_gameplayEscapeActionCancelled.Invoke;
        m_menu = null;
        m_menu_navigate = null;
        if (m_menuNavigateActionStarted != null)
            m_menu_navigate.started -= m_menuNavigateActionStarted.Invoke;
        if (m_menuNavigateActionPerformed != null)
            m_menu_navigate.performed -= m_menuNavigateActionPerformed.Invoke;
        if (m_menuNavigateActionCancelled != null)
            m_menu_navigate.cancelled -= m_menuNavigateActionCancelled.Invoke;
        m_menu_click = null;
        if (m_menuClickActionStarted != null)
            m_menu_click.started -= m_menuClickActionStarted.Invoke;
        if (m_menuClickActionPerformed != null)
            m_menu_click.performed -= m_menuClickActionPerformed.Invoke;
        if (m_menuClickActionCancelled != null)
            m_menu_click.cancelled -= m_menuClickActionCancelled.Invoke;
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
    private InputAction m_gameplay_fire;
    [SerializeField] private ActionEvent m_gameplayFireActionStarted;
    [SerializeField] private ActionEvent m_gameplayFireActionPerformed;
    [SerializeField] private ActionEvent m_gameplayFireActionCancelled;
    private InputAction m_gameplay_move;
    [SerializeField] private ActionEvent m_gameplayMoveActionStarted;
    [SerializeField] private ActionEvent m_gameplayMoveActionPerformed;
    [SerializeField] private ActionEvent m_gameplayMoveActionCancelled;
    private InputAction m_gameplay_look;
    [SerializeField] private ActionEvent m_gameplayLookActionStarted;
    [SerializeField] private ActionEvent m_gameplayLookActionPerformed;
    [SerializeField] private ActionEvent m_gameplayLookActionCancelled;
    private InputAction m_gameplay_jump;
    [SerializeField] private ActionEvent m_gameplayJumpActionStarted;
    [SerializeField] private ActionEvent m_gameplayJumpActionPerformed;
    [SerializeField] private ActionEvent m_gameplayJumpActionCancelled;
    private InputAction m_gameplay_escape;
    [SerializeField] private ActionEvent m_gameplayEscapeActionStarted;
    [SerializeField] private ActionEvent m_gameplayEscapeActionPerformed;
    [SerializeField] private ActionEvent m_gameplayEscapeActionCancelled;
    public struct GameplayActions
    {
        private DemoControls m_Wrapper;
        public GameplayActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @fire { get { return m_Wrapper.m_gameplay_fire; } }
        public ActionEvent fireStarted { get { return m_Wrapper.m_gameplayFireActionStarted; } }
        public ActionEvent firePerformed { get { return m_Wrapper.m_gameplayFireActionPerformed; } }
        public ActionEvent fireCancelled { get { return m_Wrapper.m_gameplayFireActionCancelled; } }
        public InputAction @move { get { return m_Wrapper.m_gameplay_move; } }
        public ActionEvent moveStarted { get { return m_Wrapper.m_gameplayMoveActionStarted; } }
        public ActionEvent movePerformed { get { return m_Wrapper.m_gameplayMoveActionPerformed; } }
        public ActionEvent moveCancelled { get { return m_Wrapper.m_gameplayMoveActionCancelled; } }
        public InputAction @look { get { return m_Wrapper.m_gameplay_look; } }
        public ActionEvent lookStarted { get { return m_Wrapper.m_gameplayLookActionStarted; } }
        public ActionEvent lookPerformed { get { return m_Wrapper.m_gameplayLookActionPerformed; } }
        public ActionEvent lookCancelled { get { return m_Wrapper.m_gameplayLookActionCancelled; } }
        public InputAction @jump { get { return m_Wrapper.m_gameplay_jump; } }
        public ActionEvent jumpStarted { get { return m_Wrapper.m_gameplayJumpActionStarted; } }
        public ActionEvent jumpPerformed { get { return m_Wrapper.m_gameplayJumpActionPerformed; } }
        public ActionEvent jumpCancelled { get { return m_Wrapper.m_gameplayJumpActionCancelled; } }
        public InputAction @escape { get { return m_Wrapper.m_gameplay_escape; } }
        public ActionEvent escapeStarted { get { return m_Wrapper.m_gameplayEscapeActionStarted; } }
        public ActionEvent escapePerformed { get { return m_Wrapper.m_gameplayEscapeActionPerformed; } }
        public ActionEvent escapeCancelled { get { return m_Wrapper.m_gameplayEscapeActionCancelled; } }
        public InputActionMap Get() { return m_Wrapper.m_gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
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
    private InputAction m_menu_navigate;
    [SerializeField] private ActionEvent m_menuNavigateActionStarted;
    [SerializeField] private ActionEvent m_menuNavigateActionPerformed;
    [SerializeField] private ActionEvent m_menuNavigateActionCancelled;
    private InputAction m_menu_click;
    [SerializeField] private ActionEvent m_menuClickActionStarted;
    [SerializeField] private ActionEvent m_menuClickActionPerformed;
    [SerializeField] private ActionEvent m_menuClickActionCancelled;
    public struct MenuActions
    {
        private DemoControls m_Wrapper;
        public MenuActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @navigate { get { return m_Wrapper.m_menu_navigate; } }
        public ActionEvent navigateStarted { get { return m_Wrapper.m_menuNavigateActionStarted; } }
        public ActionEvent navigatePerformed { get { return m_Wrapper.m_menuNavigateActionPerformed; } }
        public ActionEvent navigateCancelled { get { return m_Wrapper.m_menuNavigateActionCancelled; } }
        public InputAction @click { get { return m_Wrapper.m_menu_click; } }
        public ActionEvent clickStarted { get { return m_Wrapper.m_menuClickActionStarted; } }
        public ActionEvent clickPerformed { get { return m_Wrapper.m_menuClickActionPerformed; } }
        public ActionEvent clickCancelled { get { return m_Wrapper.m_menuClickActionCancelled; } }
        public InputActionMap Get() { return m_Wrapper.m_menu; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(MenuActions set) { return set.Get(); }
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
    [Serializable]
    public class ActionEvent : UnityEvent<InputAction.CallbackContext>
    {
    }
}
