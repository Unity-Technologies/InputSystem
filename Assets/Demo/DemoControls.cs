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
        // general
        m_general = asset.GetActionMap("general");
        m_general_join = m_general.GetAction("join");
        if (m_generalJoinActionStarted != null)
            m_general_join.started += m_generalJoinActionStarted.Invoke;
        if (m_generalJoinActionPerformed != null)
            m_general_join.performed += m_generalJoinActionPerformed.Invoke;
        if (m_generalJoinActionCancelled != null)
            m_general_join.cancelled += m_generalJoinActionCancelled.Invoke;
        m_Initialized = true;
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
        public ActionEvent FireStarted { get { return m_Wrapper.m_gameplayFireActionStarted; } }
        public ActionEvent FirePerformed { get { return m_Wrapper.m_gameplayFireActionPerformed; } }
        public ActionEvent FireCancelled { get { return m_Wrapper.m_gameplayFireActionCancelled; } }
        public InputAction @move { get { return m_Wrapper.m_gameplay_move; } }
        public ActionEvent MoveStarted { get { return m_Wrapper.m_gameplayMoveActionStarted; } }
        public ActionEvent MovePerformed { get { return m_Wrapper.m_gameplayMoveActionPerformed; } }
        public ActionEvent MoveCancelled { get { return m_Wrapper.m_gameplayMoveActionCancelled; } }
        public InputAction @look { get { return m_Wrapper.m_gameplay_look; } }
        public ActionEvent LookStarted { get { return m_Wrapper.m_gameplayLookActionStarted; } }
        public ActionEvent LookPerformed { get { return m_Wrapper.m_gameplayLookActionPerformed; } }
        public ActionEvent LookCancelled { get { return m_Wrapper.m_gameplayLookActionCancelled; } }
        public InputAction @jump { get { return m_Wrapper.m_gameplay_jump; } }
        public ActionEvent JumpStarted { get { return m_Wrapper.m_gameplayJumpActionStarted; } }
        public ActionEvent JumpPerformed { get { return m_Wrapper.m_gameplayJumpActionPerformed; } }
        public ActionEvent JumpCancelled { get { return m_Wrapper.m_gameplayJumpActionCancelled; } }
        public InputAction @escape { get { return m_Wrapper.m_gameplay_escape; } }
        public ActionEvent EscapeStarted { get { return m_Wrapper.m_gameplayEscapeActionStarted; } }
        public ActionEvent EscapePerformed { get { return m_Wrapper.m_gameplayEscapeActionPerformed; } }
        public ActionEvent EscapeCancelled { get { return m_Wrapper.m_gameplayEscapeActionCancelled; } }
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
        public ActionEvent NavigateStarted { get { return m_Wrapper.m_menuNavigateActionStarted; } }
        public ActionEvent NavigatePerformed { get { return m_Wrapper.m_menuNavigateActionPerformed; } }
        public ActionEvent NavigateCancelled { get { return m_Wrapper.m_menuNavigateActionCancelled; } }
        public InputAction @click { get { return m_Wrapper.m_menu_click; } }
        public ActionEvent ClickStarted { get { return m_Wrapper.m_menuClickActionStarted; } }
        public ActionEvent ClickPerformed { get { return m_Wrapper.m_menuClickActionPerformed; } }
        public ActionEvent ClickCancelled { get { return m_Wrapper.m_menuClickActionCancelled; } }
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
    // general
    private InputActionMap m_general;
    private InputAction m_general_join;
    [SerializeField] private ActionEvent m_generalJoinActionStarted;
    [SerializeField] private ActionEvent m_generalJoinActionPerformed;
    [SerializeField] private ActionEvent m_generalJoinActionCancelled;
    public struct GeneralActions
    {
        private DemoControls m_Wrapper;
        public GeneralActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @join { get { return m_Wrapper.m_general_join; } }
        public ActionEvent JoinStarted { get { return m_Wrapper.m_generalJoinActionStarted; } }
        public ActionEvent JoinPerformed { get { return m_Wrapper.m_generalJoinActionPerformed; } }
        public ActionEvent JoinCancelled { get { return m_Wrapper.m_generalJoinActionCancelled; } }
        public InputActionMap Get() { return m_Wrapper.m_general; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(GeneralActions set) { return set.Get(); }
    }
    public GeneralActions @general
    {
        get
        {
            if (!m_Initialized) Initialize();
            return new GeneralActions(this);
        }
    }
    [Serializable]
    public class ActionEvent : UnityEvent<InputAction.CallbackContext>
    {
    }
}
