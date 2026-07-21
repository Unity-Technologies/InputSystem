---
uid: input-system-default-ui-map-ref
---

# Default UI Action Map reference

The default [project-wide actions asset](./about-project-wide-actions.md) has a default configuration for UI input.

|**Action**|**Action Type**|**Control Type**|**Description**|
|:-|:-|:-|:-|
|**Navigate**|PassThrough|Vector2|A vector used to select the currently active UI [selectable](https://docs.unity3d.com/Manual/script-Selectable.html) in gamepad or arrow-key [navigation-type input](supported-ui-input-types-navigation.md).|
|**Submit**|Button|Button|Submits the currently selected UI [selectable](https://docs.unity3d.com/Manual/script-Selectable.html) in [navigation-type input](supported-ui-input-types-navigation.md)|
|**Cancel**|Button|Button|Exits any interaction with the currently selected UI [selectable](https://docs.unity3d.com/Manual/script-Selectable.html) in [navigation-type input](supported-ui-input-types-navigation.md)|
|**Point**|PassThrough|Vector2|A 2D screen position. The cursor for [pointer-type](supported-ui-input-types-pointer.md) interaction.|
|**Click**|PassThrough|Button|The primary button for [pointer-type](supported-ui-input-types-pointer.md) interaction.|
|**RightClick**|PassThrough|Button|The secondary button for [pointer-type](supported-ui-input-types-pointer.md) interaction.|
|**MiddleClick**|PassThrough|Button|The middle button for [pointer-type](supported-ui-input-types-pointer.md) interaction.|
|**ScrollWheel**|PassThrough|Vector2|The scrolling gesture for [pointer-type](supported-ui-input-types-pointer.md) interaction.|
|**Tracked Device Position**|PassThrough|Vector3|A 3D position of one or multiple spatial tracking devices, such as XR hand controllers. In combination with **Tracked Device Orientation**, this allows XR-style UI interactions by pointing at UI [selectables](https://docs.unity3d.com/Manual/script-Selectable.html) in space. Refer to [tracked-type input](supported-ui-input-types-tracked.md).|
|**Tracked Device Orientation**|PassThrough|Quaternion|a `Quaternion` representing the rotation of one or multiple spatial tracking devices, such as XR hand controllers. In combination with [Tracked Device Position](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.|html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_trackedDevicePosition), this allows XR-style UI interactions by pointing at UI [selectables](https://docs.unity3d.com/Manual/script-Selectable) in space. Refer to [tracked-type input](supported-ui-input-types-tracked.md).|
