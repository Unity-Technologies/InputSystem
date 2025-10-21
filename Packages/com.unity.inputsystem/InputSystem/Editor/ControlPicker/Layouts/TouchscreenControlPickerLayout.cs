#if UNITY_EDITOR
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal class TouchscreenControlPickerLayout : IInputControlPickerLayout
    {
        public void AddControlItem(InputControlPickerDropdown dropdown, DeviceDropdownItem parent, ControlDropdownItem parentControl,
            InputControlLayout.ControlItem control, string device, string usage, bool searchable)
        {
            // for the Press control, show two variants, one for single touch presses, and another for multi-touch presses
            if (control.displayName == "Press")
            {
                dropdown.AddControlItem(this, parent, parentControl, new InputControlLayout.ControlItem
                {
                    name = new InternedString("Press"),
                    displayName = new InternedString("Press (Single touch)"),
                    layout = control.layout
                }, device, usage, searchable);

                dropdown.AddControlItem(this, parent, parentControl, new InputControlLayout.ControlItem
                {
                    name = new InternedString("Press"),
                    displayName = new InternedString("Press (Multi-touch)"),
                    layout = control.layout
                }, device, usage, searchable, "touch*/Press");
            }
            else
            {
                dropdown.AddControlItem(this, parent, parentControl, control, device, usage, searchable);
            }
        }
    }
}
#endif // UNITY_EDITOR
