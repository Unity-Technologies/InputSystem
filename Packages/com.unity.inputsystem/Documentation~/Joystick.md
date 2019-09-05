# Joystick Support

The Input System currently has limited support for joysticks as generic [HID](HID.md) controls only. It will make a best effort to identify controls based on the information provided by the HID descriptor of the device, but that may not always be accurate. These devices often work best when allowing the user to manually [manually remap the controls](HowDoI.md#-create-a-ui-to-rebind-input-in-my-game).

To offer better support for specific joysticks devices, you can also [provide your own custom mappings for those devices](HID.md#overriding-the-hid-fallback). We hope to offer some mappings for common devices as part of the Input System package in the future.  See the [manual page on HID](HID.md) for more information.

## Controls

Generic HID input devices which are recognized as joysticks are supported via the [`Joystick`](../api/UnityEngine.InputSystem.Joystick.html) class. Joystick devices may have any number of controls as reported by the devices HID descriptor, but we always try to match at least these common controls:

|Control|Type|Description|
|-------|----|-----------|
|[`stick`](../api/UnityEngine.InputSystem.Joystick.html#UnityEngine_InputSystem_Joystick_stick)|[`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)|The main stick of the joystick.|
|[`trigger`](../api/UnityEngine.InputSystem.Joystick.html#UnityEngine_InputSystem_Joystick_trigger)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The primary trigger of the joystick.|
