---
uid: input-system-joystick
---
# Joystick support

The Input System currently has limited support for joysticks as generic [HIDs](HID.md) only. The system attempts to identify Controls based on the information provided in the HID descriptor of the Device, but it might not always be accurate. These Devices often work best when you allow the user to [manually remap the Controls](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html).

To better support specific joysticks Devices, you can also [provide your own custom mappings for those Devices](HID.md#creating-a-custom-device-layout). Unity might extend the Input System to include some mappings for common devices in the future. See the [manual page on HID](HID.md) for more information.

## Controls

The Input System supports Generic HID Input Devices which are recognized as joysticks via the [`Joystick`](../api/UnityEngine.InputSystem.Joystick.html) class. Joystick Devices can have any number of Controls as reported by the Device's HID descriptor, but the Input System always tries to at least match these common Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`stick`](../api/UnityEngine.InputSystem.Joystick.html#UnityEngine_InputSystem_Joystick_stick)|[`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)|The main stick of the joystick.|
|[`trigger`](../api/UnityEngine.InputSystem.Joystick.html#UnityEngine_InputSystem_Joystick_trigger)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The primary trigger of the joystick.|
