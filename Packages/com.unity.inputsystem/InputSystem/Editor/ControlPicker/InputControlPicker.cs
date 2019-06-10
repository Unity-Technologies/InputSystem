#if UNITY_EDITOR
using System;

////REVIEW: should this be a PopupWindowContent?

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A popup that allows picking input controls graphically.
    /// </summary>
    public class InputControlPicker
    {
        public InputControlPicker(Mode mode, Action<string> onPick, InputControlPickerState state)
        {
            m_State = state ?? new InputControlPickerState();
            m_Dropdown = new InputControlPickerDropdown(state, onPick, mode: mode);
        }

        public void Show(Rect rect)
        {
            m_Dropdown.Show(rect);
        }

        public InputControlPickerState state => m_State;

        private readonly InputControlPickerDropdown m_Dropdown;
        private readonly InputControlPickerState m_State;

        public enum Mode
        {
            PickControl,
            PickDevice,
        }
    }
}
#endif // UNITY_EDITOR
