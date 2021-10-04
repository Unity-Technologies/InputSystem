#if UNITY_EDITOR
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Editor
{
    internal class DefaultInputControlPickerLayout : IInputControlPickerLayout
    {
        public void AddControlItem(InputControlPickerDropdown dropdown, DeviceDropdownItem parent,
            ControlDropdownItem parentControl,
            InputControlLayout.ControlItem control, string device, string usage, bool searchable)
        {
            dropdown.AddControlItem(this, parent, parentControl, control, device, usage, searchable);
        }
    }
}
#endif // UNITY_EDITOR
