#if UNITY_EDITOR
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Editor
{
    internal interface IInputControlPickerLayout
    {
        void AddControlItem(InputControlPickerDropdown dropdown, DeviceDropdownItem parent,
            ControlDropdownItem parentControl,
            InputControlLayout.ControlItem control, string device, string usage, bool searchable);
    }
}
#endif // UNITY_EDITOR
