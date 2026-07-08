---
uid: input-system-enable-disable-devices
---

# Enable and disable devices

When a Device is added, the Input System sends it an initial [`QueryEnabledStateCommand`](xref:UnityEngine.InputSystem.LowLevel.QueryEnabledStateCommand) to find out whether the device is currently enabled or not. The result of this is reflected in the [`InputDevice.enabled`](xref:UnityEngine.InputSystem.InputDevice) property.

When disabled, no events other than removal ([`DeviceRemoveEvent`](xref:UnityEngine.InputSystem.LowLevel.DeviceRemoveEvent)) and configuration change ([`DeviceConfigurationEvent`](xref:UnityEngine.InputSystem.LowLevel.DeviceConfigurationEvent)) events are processed for a Device, even if they are sent.

A Device can be manually disabled and re-enabled with [`InputSystem.DisableDevice`](xref:UnityEngine.InputSystem.InputSystem) and [`InputSystem.EnableDevice`](xref:UnityEngine.InputSystem.InputSystem) respectively.

Note that [sensors](devices-sensors.md) start in a disabled state by default, and you need to enable them in order for them to generate events.

The Input System may automatically disable and re-enable Devices in certain situations, as detailed in the [next section](device-background-focus-changes.md).
