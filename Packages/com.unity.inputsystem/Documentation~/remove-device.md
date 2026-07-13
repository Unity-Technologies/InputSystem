---
uid: input-system-remove-device
---

# Remove a device

When a Device is disconnected, it is removed from the system. A notification appears for [`InputDeviceChange.Removed`](xref:UnityEngine.InputSystem.InputDeviceChange) (sent with [`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem)) and the Devices are removed from the [`devices`](xref:UnityEngine.InputSystem.InputSystem) list. The system also calls [`InputDevice.OnRemoved`](xref:UnityEngine.InputSystem.InputDevice).

The [`InputDevice.added`](xref:UnityEngine.InputSystem.InputDevice) flag is reset to false in the process.

Note that Devices are not destroyed when removed. Device instances remain valid and you can still access them in code. However, trying to read values from the controls of these Devices leads to exceptions.
