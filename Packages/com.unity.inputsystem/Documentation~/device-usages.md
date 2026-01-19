---
uid: input-system-device-usages
---

# Device usages 

Like any [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html), a Device can have usages associated with it. You can query usages with the [`usages`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_usages) property, and use[`InputSystem.SetDeviceUsage()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_SetDeviceUsage_UnityEngine_InputSystem_InputDevice_System_String_) to set them. Usages can be arbitrary strings with arbitrary meanings. One common case where the Input System assigns Devices usages is the handedness of XR controllers, which are tagged with the "LeftHand" or "RightHand" usages.
