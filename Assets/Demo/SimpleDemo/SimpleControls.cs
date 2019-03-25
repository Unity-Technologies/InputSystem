// GENERATED AUTOMATICALLY FROM 'Assets/Demo/SimpleDemo/SimpleControls.inputactions'

using System;
using UnityEngine;
using UnityEngine.Experimental.Input;


public class SimpleControls
{
    public SimpleControls()
    {
        // gameplay
        m_gameplay = new InputActionMap("gameplay");
        m_gameplay_fire = m_gameplay.AddAction("fire");
        m_gameplay_fire.AddBinding("*/{PrimaryAction}", interactions: "Tap,SlowTap");
        m_gameplay_fire.AddBinding("<SteamDemoController>/fire");
        m_gameplay_move = m_gameplay.AddAction("move");
        m_gameplay_move.AddBinding("<Gamepad>/leftStick");
        m_gameplay_move.AddCompositeBinding("Dpad").With("up", "<Keyboard>/w").With("down", "<Keyboard>/s").With("left", "<Keyboard>/a").With("right", "<Keyboard>/d");
        m_gameplay_move.AddBinding("<SteamDemoController>/move");
        m_gameplay_look = m_gameplay.AddAction("look");
        m_gameplay_look.AddBinding("<Gamepad>/rightStick");
        m_gameplay_look.AddBinding("<Pointer>/delta").WithProcessor("ScaleVector2(x=2,y=2)");
        m_gameplay_look.AddBinding("<SteamDemoController>/look");
        m_gameplay_jump = m_gameplay.AddAction("jump");
        m_gameplay_jump.AddBinding("<Keyboard>/space");
        m_gameplay_jump.AddBinding("<Gamepad>/buttonNorth");
    }
    public void Enable()
    {
        m_gameplay.Enable();
    }
    public void Disable()
    {
        m_gameplay.Disable();
    }
    // gameplay
    private InputActionMap m_gameplay;
    private InputAction m_gameplay_fire;
    private InputAction m_gameplay_move;
    private InputAction m_gameplay_look;
    private InputAction m_gameplay_jump;
    public struct GameplayActions
    {
        private SimpleControls m_Wrapper;
        public GameplayActions(SimpleControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @fire { get { return m_Wrapper.m_gameplay_fire; } }
        public InputAction @move { get { return m_Wrapper.m_gameplay_move; } }
        public InputAction @look { get { return m_Wrapper.m_gameplay_look; } }
        public InputAction @jump { get { return m_Wrapper.m_gameplay_jump; } }
        public InputActionMap Get() { return m_Wrapper.m_gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled { get { return Get().enabled; } }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
    }
    public GameplayActions @gameplay
    {
        get
        {
            return new GameplayActions(this);
        }
    }
}
