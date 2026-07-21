---
uid: input-system-switch-gamepads
---

# Switch gamepads

The Input System supports Switch Pro controllers on desktop computers with the [`SwitchProControllerHID`](xref:UnityEngine.InputSystem.Switch.SwitchProControllerHID) class, which implements basic gamepad functionality.

This support doesn't currently work for Switch Pro controllers connected with wired USB. Instead, the Switch Pro controller must be connected with Bluetooth. This is due to the controller using a proprietary communication protocol [on top of HID](hid-specification-introduction.md) which doesn't allow treating the controller like any other HID.

For more information on platform support for Switch gamepads, refer to [Supported devices reference](supported-devices-reference.md).
