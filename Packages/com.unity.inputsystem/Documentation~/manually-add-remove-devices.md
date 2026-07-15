---
uid: input-system-manually-add-remove-devices
---

# Manually add and remove devices

To manually add and remove Devices through the API, use [`InputSystem.AddDevice()`](xref:UnityEngine.InputSystem.InputSystem) and [`InputSystem.RemoveDevice()`](xref:UnityEngine.InputSystem.InputSystem).

This allows you to create your own Devices, which can be useful for testing purposes, or for creating virtual Input Devices which synthesize input from other events. As an example, see the [on-screen Controls](on-screen-controls.md) that the Input System provides. The Input Devices used for on-screen Controls are created entirely in code and have no [native representation](native-devices.md).
