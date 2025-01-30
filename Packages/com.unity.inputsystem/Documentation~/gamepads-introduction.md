# Gamepads introduction

A [Gamepad](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Gamepad.html) is defined as a device with two thumbsticks, a D-pad, and four face buttons. Additionally, gamepads usually have two shoulder and two trigger buttons. Most gamepads also have two buttons in the middle.

A gamepad can have additional controls, such as a gyroscope, which the device can expose. However, all gamepads are guaranteed to have at least the minimum set of controls defined in the properties of the [Gamepad](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Gamepad.html) class.

The Input System additionally has specific APIs available for the following devices:

* PlayStation DualShock and DualSense devices, in the [DualShock](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.DualShock.html) namespace.  
* Xbox XInput devices, in the [XInput](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.XInput.html) namespace.  
* Switch Pro controllers, in the [Switch](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Switch.html) namespace.

For a list of platforms that support gamepads, refer to \[Platform support\].

Gamepad support guarantees the correct location and functioning of controls across platforms and hardware. For example, a PS4 DualShock controller layout should look identical regardless of which platform it is supported on. A gamepad's south face button should always be the lowermost face button.

Important: Generic [HID](http://localhost:57437/com.unity.inputsystem@1.12/manual/HID.html) gamepads aren't surfaced as [Gamepad](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Gamepad.html) devices and are created as generic [joysticks](http://localhost:57437/com.unity.inputsystem@1.12/manual/Joystick.html). This is because the Input System can't guarantee correct mapping of buttons and axes on the controller. Only HID gamepads that are explicitly supported by the Input System (like the PlayStation 4 controller) are defined as gamepads. To set up the same kind of support for specific HID gamepads yourself refer to [Overriding the HID Fallback](http://localhost:57437/com.unity.inputsystem@1.12/manual/HID.html#creating-a-custom-device-layout).
