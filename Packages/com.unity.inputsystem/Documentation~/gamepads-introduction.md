# Gamepads introduction

A gamepad is defined as a device with two thumbsticks, a D-pad, and four face buttons. Additionally, gamepads usually have two shoulder and two trigger buttons. Most gamepads also have two buttons in the middle.

A gamepad can have additional controls, such as a gyroscope, which the device can expose. However, all gamepads have at least the minimum set of controls defined in the properties of the [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) class.

The Input System additionally has specific APIs available for the following devices:

* PlayStation DualShock and DualSense devices, in the [`DualShock`](xref:UnityEngine.InputSystem.DualShock) namespace.  
* Xbox XInput devices, in the [`XInput`](xref:UnityEngine.InputSystem.XInput) namespace.  
* Switch Pro controllers, in the [`Switch`](xref:UnityEngine.InputSystem.Switch) namespace.

For a list of platforms that support gamepads, refer to [Supported devices reference](supported-devices-reference.md).

Gamepad support guarantees the correct location and functioning of controls across platforms and hardware. For example, a PlayStation 4 DualShock controller layout is identical regardless of which platform it's used on. A gamepad's south face button is always be the lowermost face button.

> [!IMPORTANT] 
> Generic [HID](hid-specification.md) gamepads aren't surfaced as [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) devices and are created as generic [joysticks](devices-joysticks.md). This is because the Input System can't guarantee correct mapping of buttons and axes on the controller. Only HID gamepads that are explicitly supported by the Input System (like the PlayStation 4 controller) are defined as gamepads. To set up the same kind of support for specific HID gamepads yourself refer to [Create a custom device layout](hid-create-custom-layout.md).
