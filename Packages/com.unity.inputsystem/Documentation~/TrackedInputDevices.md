---
uid: input-system-tracked-input-devices
---
# Tracked Input Devices

Some input devices can provide information about their spatial position and orientation. It can then be used to pose other objects in your scene, or to interact with your scene. 

## Tracked Pose Driver

The Tracked Pose Driver component is used to synchronize a GameObject's transform with input data from a tracked device such as an XR headset, motion controller, or other devices that provide positional and rotational tracking. It allows creating immersive XR experiences by dynamically updating the GameObject’s position and rotation based on the real-world movement of the tracked device. The component works with Unity's Input System to gather position, rotation, and tracking state data, and it provides several customization options, such as updating specific transform properties, controlling update timing, and managing how invalid tracking data is handled.

|Property Name|Description|
|-------------|-----------|
|[`Tracking Type`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_trackingType)|Specifies which transform properties (position, rotation, or both) should be updated based on the tracked data.|
|[`Update Type`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_updateType)|Determines when updates to the transform occur within Unity's event loop, such as during rendering or gameplay.|
|[`Ignore Tracking State`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_ignoreTrackingState)|If enabled, ignores the tracking state and always assumes the input pose is valid, even when flagged otherwise.|
|[`Position Input`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_positionInput)|An input action used to retrieve the position data (Vector3) of the tracked device.|
|[`Rotation Input`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_rotationInput)|An input action used to retrieve the rotation data (Quaternion) of the tracked device.|
|[`Tracking State Input`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_trackingStateInput)|An input action used to determine whether the tracking state (position or rotation) is valid (Integer).|

## Tracked Device Raycaster

Tracked Device Raycaster enables raycasting from tracked input devices, such as XR controllers, to interact with UI elements rendered in 3D space. Designed to work alongside the Canvas component, this raycaster replaces the standard GraphicRaycaster for XR and AR use cases, allowing pointer events to be processed based on ray intersections with graphics in world space. The component supports occlusion checks, custom ray distance limits, and filtering of reversed graphics to provide reliable UI interaction in immersive environments. It is useful for building XR-enabled user interfaces where traditional 2D input methods, such as a mouse, are not applicable.

|Property Name|Description|
|-------------|-----------|
|[`Ignore Reversed Graphics`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_ignoreReversedGraphics)|If enabled, ignores graphics whose normal faces away from the ray’s direction.|
|[`Check For 2D Occlusion`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_checkFor2DOcclusion)|Enables occlusion checks for 2D objects, such as sprites in the scene.|
|[`Check For 3D Occulusion`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_checkFor3DOcclusion)|Enables occlusion checks for 3D objects, preventing rays from passing through physical geometry.|
|[`Max Distance`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_maxDistance)|Sets the maximum ray distance for interaction detection in world space coordinates.|
|[`Blocking Mask`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_blockingMask)|Defines the layer mask used to check for occlusion when performing raycasting.|
