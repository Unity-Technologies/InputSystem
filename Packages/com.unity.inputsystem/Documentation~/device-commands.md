---
uid: input-system-device-commands
---

# Device commands

Send data to Devices from script—for rumble, HID output reports, and other backend features.

Unlike [input events](input-events.md), which flow from hardware into the Input System, commands flow out to the Device. Send built-in command types or define your own for custom hardware.

| **Topic** | **Description** |
| :--- | :--- |
| **[Send a command to a device](send-command-to-device.md)** | Call `InputDevice.ExecuteCommand` and monitor commands with `InputSystem.onDeviceCommand`. |
| **[Add a custom device command](add-custom-device-command.md)** | Define `IInputDeviceCommandInfo` structs to send custom data to a Device backend. |

## Additional resources

- [Input events](input-events.md)
- [Human Interface Device specification](hid-specification.md)
- [Gamepad haptics](gamepad-haptics.md)
- [Step 6 Device Commands (Optional)](step-6-device-commands-optional.md)
