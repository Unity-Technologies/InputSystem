---
uid: input-system-xbox-gamepads
---

# Xbox gamepads

The Input System implements Xbox gamepads using the [`XInputController`](xref:UnityEngine.InputSystem.XInput.XInputController) class, which derives from [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad). On Windows and Universal Windows Platform, Unity uses the `XInput` API to connect to any type of supported XInput controller, including all Xbox One or Xbox 360-compatible controllers. These controllers are represented as an [`XInputController`](xref:UnityEngine.InputSystem.XInput.XInputController) instance. 

You can query the [`XInputController.subType`](xref:UnityEngine.InputSystem.XInput.XInputController.subType) property to get information about the type of controller (for example, a wheel or a gamepad).

On other platforms, Unity uses derived classes to represent Xbox controllers:

* [`XboxGamepadMacOS`](xref:UnityEngine.InputSystem.XInput.XboxGamepadMacOS): Any Xbox or compatible gamepad connected to macOS via USB using the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller).  
* [`XboxOneGampadMacOSWireless`](xref:UnityEngine.InputSystem.XInput.XboxOneGampadMacOSWireless): An Xbox One controller connected to macOS via Bluetooth. Only the latest generation of Xbox One controllers supports Bluetooth. These controllers don't require any additional drivers in this scenario.  
* [`XboxOneGampadiOS`](xref:UnityEngine.InputSystem.iOS.XboxOneGampadiOS): An Xbox One controller connected to an iOS device via Bluetooth. Requires iOS 13 or higher.

XInput controllers on macOS require the installation of the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller). This driver only supports USB connections, and doesn't support wireless dongles. However, the latest generation of Xbox One controllers natively support Bluetooth. macOS natively supports these controllers as [HID devices](hid-specification-introduction.md) without any additional drivers when connected via Bluetooth.

Unity supports Xbox controllers on WebGL in some browser and OS configurations, but treats them as basic [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) or [`Joystick`](xref:UnityEngine.InputSystem.Joystick) devices, and doesn't support rumble or any other Xbox-specific functionality.

For more information on platform support for Xbox gamepads, refer to [Supported devices reference](supported-devices-reference.md).