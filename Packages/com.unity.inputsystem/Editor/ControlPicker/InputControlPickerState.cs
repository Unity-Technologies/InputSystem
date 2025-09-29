#if UNITY_EDITOR || PACKAGE_DOCS_GENERATION
using System;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Persistent state for <see cref="InputControlPathEditor"/>.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the viewing state for an input control picker.
    /// </remarks>
    [Serializable]
    public class InputControlPickerState
    {
        internal AdvancedDropdownState advancedDropdownState => m_AdvancedDropdownState;

        internal bool manualPathEditMode
        {
            get => m_ManualPathEditMode;
            set => m_ManualPathEditMode = value;
        }

        [SerializeField] private AdvancedDropdownState m_AdvancedDropdownState = new AdvancedDropdownState();
        [SerializeField] private bool m_ManualPathEditMode;
    }
}
#endif
