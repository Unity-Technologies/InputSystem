---
uid: input-system-manually-add-remove-devices
---

# Manually add and remove devices 

To manually add and remove Devices through the API, use [`InputSystem.AddDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice_UnityEngine_InputSystem_InputDevice_) and [`InputSystem.RemoveDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RemoveDevice_UnityEngine_InputSystem_InputDevice_).

This allows you to create your own Devices, which can be useful for testing purposes, or for creating virtual Input Devices which synthesize input from other events. As an example, see the [on-screen Controls](OnScreen.md) that the Input System provides. The Input Devices used for on-screen Controls are created entirely in code and have no [native representation](#native-devices).
