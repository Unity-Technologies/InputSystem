// GENERATED AUTOMATICALLY FROM 'Assets/Demo/DemoControls.inputactions'

[System.Serializable]
public class DemoControls : UnityEngine.Experimental.Input.InputActionWrapper
{
    private bool m_Initialized;
    private void Initialize()
    {
        // gameplay
        m_gameplay = asset.GetActionMap("gameplay");
        m_gameplay_fire = m_gameplay.GetAction("fire");
        m_gameplay_move = m_gameplay.GetAction("move");
        m_gameplay_look = m_gameplay.GetAction("look");
        m_Initialized = true;
    }

    // gameplay
    private UnityEngine.Experimental.Input.InputActionMap m_gameplay;
    private UnityEngine.Experimental.Input.InputAction m_gameplay_fire;
    private UnityEngine.Experimental.Input.InputAction m_gameplay_move;
    private UnityEngine.Experimental.Input.InputAction m_gameplay_look;
    public struct GameplayActions
    {
        private DemoControls m_Wrapper;
        public GameplayActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public UnityEngine.Experimental.Input.InputAction @fire { get { return m_Wrapper.m_gameplay_fire; } }
        public UnityEngine.Experimental.Input.InputAction @move { get { return m_Wrapper.m_gameplay_move; } }
        public UnityEngine.Experimental.Input.InputAction @look { get { return m_Wrapper.m_gameplay_look; } }
        public UnityEngine.Experimental.Input.InputActionMap Get() { return m_Wrapper.m_gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public UnityEngine.Experimental.Input.InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator UnityEngine.Experimental.Input.InputActionMap(GameplayActions set) { return set.Get(); }
    }
    public GameplayActions @gameplay
    {
        get
        {
            if (!m_Initialized) Initialize();
            return new GameplayActions(this);
        }
    }
}
