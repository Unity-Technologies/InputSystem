---
uid: input-system-send-command-to-device
---

# Send a command to a device

The Input System sends commands to the Device through [`InputDevice.ExecuteCommand<TCommand>`](xref:UnityEngine.InputSystem.InputDevice). To monitor Device commands, use [`InputSystem.onDeviceCommand`](xref:UnityEngine.InputSystem.InputSystem).

Each Device command implements the [`IInputDeviceCommandInfo`](xref:UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo) interface, which only requires the [`typeStatic`](xref:UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo) property to identify the type of the command. The native implementation of the Device should then understand how to handle that command. One common case is the `"HIDO"` command type which is used to send [HID output reports](hid-specification-introduction.md) to HIDs.
