// GENERATED AUTOMATICALLY FROM 'Assets/Demo/SimpleDemo/SimpleControls.inputactions'

using System;
using UnityEngine;
using UnityEngine.Experimental.Input;


[Serializable]
public class SimpleControls : InputActionAssetReference
{
    public SimpleControls()
    {
    }

    public SimpleControls(InputActionAsset asset)
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
    private InputAction m_gameplay_move;
    private InputAction m_gameplay_look;
    private InputAction m_gameplay_jump;
    private InputAction m_gameplay_escape;
    public struct GameplayActions
    {
        private SimpleControls m_Wrapper;
        public GameplayActions(SimpleControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @fire { get { return m_Wrapper.m_gameplay_fire; } }
        public InputAction @move { get { return m_Wrapper.m_gameplay_move; } }
        public InputAction @look { get { return m_Wrapper.m_gameplay_look; } }
        public InputAction @jump { get { return m_Wrapper.m_gameplay_jump; } }
        public InputAction @escape { get { return m_Wrapper.m_gameplay_escape; } }
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
    private int m_KeyboardMouseSchemeIndex = -1;
    public InputControlScheme KeyboardMouseScheme
    {
        get
        {
            if (m_KeyboardMouseSchemeIndex == -1) m_KeyboardMouseSchemeIndex = asset.GetControlSchemeIndex("Keyboard&Mouse");
            return asset.controlSchemes[m_KeyboardMouseSchemeIndex];
        }
    }
}
