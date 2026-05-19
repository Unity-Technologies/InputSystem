---
uid: input-system-tracked-ui-support
---

# Tracked input UI support

Input from [tracked devices](../api/UnityEngine.InputSystem.TrackedDevice.html) such as [XR controllers](../api/UnityEngine.InputSystem.XR.XRController.html) and [HMDs](../api/UnityEngine.InputSystem.XR.XRHMD.html) behaves like [pointer-type input](supported-ui-input-types-pointer.md), but the Input System uses raycasting to translate the world-space device position and orientation sourced from [`trackedDevicePosition`](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_trackedDevicePosition) and [`trackedDeviceOrientation`](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_trackedDeviceOrientation) into a screen-space position.

>[!IMPORTANT]
>Because multiple tracked devices can feed into the same set of actions, it is important to set the [action type](./respond-to-input.md#action-types) to [PassThrough](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_PassThrough). This ensures that the Input System applies no filtering on these input actions, and relays inputs correctly.

For this raycasting to work, you need to add [TrackedDeviceRaycaster](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html) to the `GameObject` that has the UI's `Canvas` component. This `GameObject` will usually have a `GraphicRaycaster` component which, however, only works for 2D screen-space raycasting. You can put [TrackedDeviceRaycaster](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html) alongside `GraphicRaycaster` and both can be enabled at the same time without advserse effect.

![TrackedDeviceRayster Add Component](Images/TrackedDeviceRaycasterComponentMenu.png)

![TrackedDeviceRayster Properties](Images/TrackedDeviceRaycaster.png)

Clicks on tracked devices do not differ from other [pointer-type input](supported-ui-input-types-pointer.md). Therefore, actions such as [leftClick](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_leftClick) work for tracked devices just like they work for other pointers.