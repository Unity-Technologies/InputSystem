---
uid: input-system-device-usages
---

# Device usages

Like any [`InputControl`](xref:UnityEngine.InputSystem.InputControl), a Device can have usages associated with it. You can query usages with the [`usages`](xref:UnityEngine.InputSystem.InputControl) property, and use[`InputSystem.SetDeviceUsage()`](xref:UnityEngine.InputSystem.InputSystem) to set them. Usages can be arbitrary strings with arbitrary meanings. One common case where the Input System assigns Devices usages is the handedness of XR controllers, which are tagged with the "LeftHand" or "RightHand" usages.
