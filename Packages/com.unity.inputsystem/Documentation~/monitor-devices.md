---
uid: input-system-monitor-devices
---

# Monitor devices

To be notified when new Devices are added or existing Devices are removed, use [`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem).

```CSharp
InputSystem.onDeviceChange +=
    (device, change) =>
    {
        switch (change)
        {
            case InputDeviceChange.Added:
                // New Device.
                break;
            case InputDeviceChange.Disconnected:
                // Device got unplugged.
                break;
            case InputDeviceChange.Connected:
                // Plugged back in.
                break;
            case InputDeviceChange.Removed:
                // Remove from Input System entirely; by default, Devices stay in the system once discovered.
                break;
            default:
                // See InputDeviceChange reference for other event types.
                break;
        }
    }
```

[`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem) delivers notifications for other device-related changes as well. See the [`InputDeviceChange` enum](xref:UnityEngine.InputSystem.InputDeviceChange) for more information.
