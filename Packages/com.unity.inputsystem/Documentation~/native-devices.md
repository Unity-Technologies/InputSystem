---
uid: input-system-native-devices
---

# Native devices

Devices that the [native backend](Architecture.md#native-backend) reports are considered native (as opposed to Devices created from script code). To identify these Devices, you can check the [`InputDevice.native`](xref:UnityEngine.InputSystem.InputDevice) property.

The Input System remembers native Devices. For example, if the system has no matching layout when the Device is first reported, but a layout which matches the device is registered later, the system uses this layout to recreate the Device.

You can force the Input System to use your own [layout](layouts.md) when the native backend discovers a specific Device, by describing the Device in the layout, like this:

```
     {
        "name" : "MyGamepad",
        "extend" : "Gamepad",
        "device" : {
            // All strings in here are regexs and case-insensitive.
            "product" : "MyController",
            "manufacturer" : "MyCompany"
        }
     }
```

Note: You don't have to restart Unity in order for changes in your layout to take effect on native Devices. The Input System applies changes automatically on every domain reload, so you can just keep refining a layout and your Device is recreated with the most up-to-date version every time scripts are recompiled.


## Disconnected Devices

If you want to get notified when Input Devices disconnect, subscribe to the [`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem) event, and look for events of type [`InputDeviceChange.Disconnected`](xref:UnityEngine.InputSystem.InputDeviceChange).

The Input System keeps track of disconnected Devices in [`InputSystem.disconnectedDevices`](xref:UnityEngine.InputSystem.InputSystem). If one of these Devices reconnects later, the Input System can detect that the Device was connected before, and reuses its [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) instance. This allows the [`PlayerInputManager`](player-input-manager-component.md) to reassign the Device to the same [user](user-management.md) again.
