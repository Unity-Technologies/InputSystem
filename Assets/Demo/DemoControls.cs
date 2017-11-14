// GENERATED AUTOMATICALLY FROM 'Assets/Demo/DemoControls.inputactions'

[System.Serializable]
public class DemoControls : ISX.InputActionWrapper
{
    private bool m_Initialized;
    private void Initialize()
    {
        // gameplay
        m_gameplay = asset.GetActionSet("gameplay");
        m_gameplay_fire = m_gameplay.GetAction("fire");
        m_gameplay_move = m_gameplay.GetAction("move");
        m_gameplay_look = m_gameplay.GetAction("look");
        m_Initialized = true;
    }

    // gameplay
    private ISX.InputActionSet m_gameplay;
    private ISX.InputAction m_gameplay_fire;
    private ISX.InputAction m_gameplay_move;
    private ISX.InputAction m_gameplay_look;
    public struct GameplayActions
    {
        private DemoControls m_Wrapper;
        public GameplayActions(DemoControls wrapper) { m_Wrapper = wrapper; }
        public ISX.InputAction @fire { get { return m_Wrapper.m_gameplay_fire; } }
        public ISX.InputAction @move { get { return m_Wrapper.m_gameplay_move; } }
        public ISX.InputAction @look { get { return m_Wrapper.m_gameplay_look; } }
        public ISX.InputActionSet Get() { return m_Wrapper.m_gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public ISX.InputActionSet Clone() { return Get().Clone(); }
        public static implicit operator ISX.InputActionSet(GameplayActions set) { return set.Get(); }
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
