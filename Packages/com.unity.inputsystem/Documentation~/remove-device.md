---
uid: input-system-remove-device
---

# Remove a device 

When a Device is disconnected, it is removed from the system. A notification appears for [`InputDeviceChange.Removed`](../api/UnityEngine.InputSystem.InputDeviceChange.html) (sent via [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange)) and the Devices are removed from the [`devices`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange) list. The system also calls [`InputDevice.OnRemoved`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_OnRemoved_).

The [`InputDevice.added`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_added) flag is reset to false in the process.

Note that Devices are not destroyed when removed. Device instances remain valid and you can still access them in code. However, trying to read values from the controls of these Devices leads to exceptions.
