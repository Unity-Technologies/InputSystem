---
uid: input-system-enable-disable-devices
---

# Enable and disable devices 

When a Device is added, the Input System sends it an initial [`QueryEnabledStateCommand`](../api/UnityEngine.InputSystem.LowLevel.QueryEnabledStateCommand.html) to find out whether the device is currently enabled or not. The result of this is reflected in the [`InputDevice.enabled`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_enabled) property.

When disabled, no events other than removal ([`DeviceRemoveEvent`](../api/UnityEngine.InputSystem.LowLevel.DeviceRemoveEvent.html)) and configuration change ([`DeviceConfigurationEvent`](../api/UnityEngine.InputSystem.LowLevel.DeviceConfigurationEvent.html)) events are processed for a Device, even if they are sent.

A Device can be manually disabled and re-enabled via [`InputSystem.DisableDevice`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_DisableDevice_) and [`InputSystem.EnableDevice`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_EnableDevice_) respectively.

Note that [sensors](devices-sensors.md) start in a disabled state by default, and you need to enable them in order for them to generate events.

The Input System may automatically disable and re-enable Devices in certain situations, as detailed in the [next section](#background-and-focus-change-behavior).
