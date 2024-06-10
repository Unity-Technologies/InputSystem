namespace UnityEngine.InputSystem.Experimental
{
    public class View
    {
        private uint m_LowerBound;
        private uint m_UpperBound;
        
        public View(uint lowerBound = 0, uint upperBound = 0)
        {
            m_LowerBound = lowerBound;
            m_UpperBound = upperBound;
        }
        
        public void Tick()
        {
            m_LowerBound = m_UpperBound;
            m_UpperBound = Host.tick + 1;
        }

        public uint lowerBound { get; private set; }
        public uint upperBound { get; private set; }
        public uint length => upperBound - lowerBound;
    }
}