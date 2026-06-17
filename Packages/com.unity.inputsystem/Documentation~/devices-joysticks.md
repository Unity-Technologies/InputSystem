---
uid: input-system-joystick
---
# Joystick support

The Input System currently supports joysticks as generic [HIDs](hid-specification.md) only. The system attempts to identify controls based on the information provided in the HID descriptor of the device, but it might not always be accurate. These devices often work best when you allow the user to [manually remap the controls](xref:UnityEngine.InputSystem.InputActionRebindingExtensions).

To better support specific joysticks devices, you can also [provide your own custom mappings for those devices](hid-create-custom-layout.md). For more information, refer to the [HID](hid-specification.md) documentation.

## Controls

The Input System supports generic HID Input devices which are recognized as joysticks with the [`Joystick`](xref:UnityEngine.InputSystem.Joystick) class. Joystick devices can have any number of controls as reported by the Device's HID descriptor.
