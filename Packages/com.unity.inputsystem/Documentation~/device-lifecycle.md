---
uid: input-system-device-lifecycle
---

# Device lifecycle

Manage how Input Devices are created, updated, and torn down at runtime and in the Editor.

Use these topics when you need to add or remove devices, reset or sync state, control whether devices process input, or understand focus and domain reload behavior.

| **Topic** | **Description** |
| :--- | :--- |
| **[Create a device](create-device.md)** | Add devices with `InputSystem.AddDevice` and understand automatic layout instantiation. |
| **[Remove a device](remove-device.md)** | Remove disconnected or manual devices and handle `onDeviceChange` notifications. |
| **[Reset a device](reset-device.md)** | Reset controls to default state with `InputSystem.ResetDevice`. |
| **[Sync a device](sync-device.md)** | Request current hardware state with `RequestSyncCommand` when the platform supports it. |
| **[Enable and disable devices](enable-disable-devices.md)** | Control whether a device processes input using enabled state and commands. |
| **[Device background and focus changes](device-background-focus-changes.md)** | Handle focus loss, regain, and background execution settings for device state. |
| **[Devices and domain reloads](devices-domain-reloads.md)** | Understand how Editor domain reloads recreate devices and reset state. |

## Additional resources

- [Working with devices](working-with-devices.md)
- [Device states](device-states.md)
- [Native devices](native-devices.md)
- [Debug a device](debug-device.md)
