---
uid: input-system-device-lifecycle
---

# Device lifecycle 

Use this section to script or debug behaviour beyond reading the current device or binding [actions](actions.md). 

This section describes how [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) instances enter and leave the Input System, how you can reset or synchronize their state, enable or disable them, and how focus, background execution, and Editor domain reloads affect which devices exist and what state they hold. 

| **Topic** | **Description** |
| :--- | :--- |
| [**Create a device**](create-device.md) | Learn how to add a new device with `InputSystem.AddDevice`. |
| [**Remove a device**](remove-device.md) | Learn how to remove devices from the Input System and what happens to their state and associated bindings. |
| [**Reset a device**](reset-device.md) | Learn how to reset device controls to their default state, and what happens to the device's actions. |
| [**Sync a device**](sync-device.md) | Learn how to request state synchronization so a device reflects current hardware state before processing input. |
| [**Enable and disable devices**](enable-disable-devices.md) | Learn how to temporarily disable or re-enable devices and how that affects event processing and actions. |
| [**Device background and focus changes**](device-background-focus-changes.md) | Learn how focus loss, regain, and background execution settings influence device availability and state updates. |
| [**Devices and domain reloads**](devices-domain-reloads.md) | Learn how Editor domain reloads affect device instances and what to expect when scripts recompile. |

## Additional resources

