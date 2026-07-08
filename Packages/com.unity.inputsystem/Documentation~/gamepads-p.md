---
uid: input-system-playstation-gamepads
---

# PlayStation gamepads

The Input System implements PlayStation gamepads as different derived types of the [`DualShockGamepad`](xref:UnityEngine.InputSystem.DualShock.DualShockGamepad) base class, which derives from [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad):

* [`DualShock3GamepadHID`](xref:UnityEngine.InputSystem.DualShock.DualShock3GamepadHID): A DualShock 3 controller connected to a desktop computer using the [HID interface](hid-specification-introduction.md). Currently only supported on macOS. Doesn't support [rumble](gamepad-haptics.md).
* [`DualShock4GamepadHID`](xref:UnityEngine.InputSystem.DualShock.DualShock4GamepadHID): A DualShock 4 controller connected to a desktop computer using the [HID interface](hid-specification-introduction.md). Supported on macOS, Windows, UWP, and Linux.
* [`DualSenseGamepadHID`](xref:UnityEngine.InputSystem.DualShock.DualSenseGamepadHID): A DualSense controller connected to a desktop computer using the [HID interface](hid-specification-introduction.md). Supported on macOS, Windows.
* [`DualShock4GampadiOS`](xref:UnityEngine.InputSystem.iOS.DualShock4GampadiOS): A DualShock 4 controller connected to an iOS Device with Bluetooth. Requires iOS 13 or higher.
* [`SetLightBarColor(Color)`](xref:UnityEngine.InputSystem.DualShock.DualShockGamepad.SetLightBarColor(UnityEngine.Color)): Used to set the color of the light bar on the controller.

Unity supports PlayStation controllers on WebGL in some browser and OS configurations, but treats them as basic [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) or [`Joystick`](xref:UnityEngine.InputSystem.Joystick) devices, and doesn't support rumble or any other DualShock-specific functionality.

Unity doesn't support connecting a PlayStation controller to a desktop machine using the DualShock 4 USB wireless adaptor. Use USB or Bluetooth to connect it. For more information on platform support for PlayStation gamepads, refer to [Supported devices reference](supported-devices-reference.md).

## Input output control commands

Because of limitations in the USB driver and the hardware, the Input System can only service one IOCTL (input/output control) command at a time. [`SetLightBarColor(Color)`](xref:UnityEngine.InputSystem.DualShock.DualShockGamepad.SetLightBarColor(UnityEngine.Color)) and [`SetMotorSpeeds(Single, Single)`](xref:UnityEngine.InputSystem.Gamepad.SetMotorSpeeds(System.Single,System.Single)) functionality on Dualshock 4 is implemented using IOCTL commands, so if either method is called in quick succession, it's likely that only the first command will successfully complete. The other commands are dropped.

If you need to set both lightbar color and rumble motor speeds at the same time, use the [`SetMotorSpeedsAndLightBarColor(Single, Single, Color)`](xref:UnityEngine.InputSystem.DualShock.DualShock4GamepadHID.SetMotorSpeedsAndLightBarColor(System.Single,System.Single,UnityEngine.Color)) method.
