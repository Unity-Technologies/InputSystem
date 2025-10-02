---
uid: input-system-joystick
---
# Joystick support

The Input System currently has limited support for joysticks as generic [HIDs](xref:input-system-hid) only. The system attempts to identify Controls based on the information provided in the HID descriptor of the Device, but it might not always be accurate. These Devices often work best when you allow the user to [manually remap the Controls](xref:UnityEngine.InputSystem.InputActionRebindingExtensions).

To better support specific joysticks Devices, you can also [provide your own custom mappings for those Devices](xref:input-system-hid#creating-a-custom-device-layout). Unity might extend the Input System to include some mappings for common devices in the future. See the [manual page on HID](xref:input-system-hid) for more information.

## Controls

The Input System supports Generic HID Input Devices which are recognized as joysticks via the [`Joystick`](xref:UnityEngine.InputSystem.Joystick) class. Joystick Devices can have any number of Controls as reported by the Device's HID descriptor, but the Input System always tries to at least match these common Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`stick`](xref:UnityEngine.InputSystem.Joystick.stick)|[`StickControl`](xref:UnityEngine.InputSystem.Controls.StickControl)|The main stick of the joystick.|
|[`trigger`](xref:UnityEngine.InputSystem.Joystick.trigger)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The primary trigger of the joystick.|
