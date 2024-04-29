using System;

namespace UnityEngine.InputSystem
{
    [Serializable]
    struct BindingPreset
    {
        public BindingPreset(string displayName, Action<InputAction> applyPreset)
        {
            m_Name = displayName;
            m_ApplyPreset = applyPreset;
        }

        public string Name => m_Name;
    
        public void Apply(InputAction action)
        {
            m_ApplyPreset(action);
        }
    
        [SerializeField] private readonly Action<InputAction> m_ApplyPreset;
        [SerializeField] private readonly string m_Name;
    }
}


