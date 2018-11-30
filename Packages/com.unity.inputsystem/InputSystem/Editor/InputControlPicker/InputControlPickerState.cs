#if UNITY_EDITOR
using System;

namespace UnityEngine.Experimental.Input.Editor
{
    [Serializable]
    public class InputControlPickerState
    {
        [SerializeField]
        AdvancedDropdownState m_State = new AdvancedDropdownState();
        internal AdvancedDropdownState state
        {
            get
            {
                return m_State;
            }
        }
    }
}
#endif
