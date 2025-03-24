---
uid: input-system-tracked-input-devices
---
# Tracked Input Devices

Some input devices can provide information about their spatial position and orientation. It can then be used to pose other objects in your scene, or to interact with your scene.

## Tracked Pose Driver

Use the Tracked Pose Driver component to synchronize a __GameObject__'s transform with input data from a tracked device such as an XR headset, motion controller, or other devices that provide positional and rotational tracking. It allows creating immersive XR experiences by dynamically updating the __GameObject__’s position and rotation based on the real-world movement of the tracked device. The component works with Unity's Input System to gather position, rotation, and tracking state data, and it provides several customization options, such as updating specific transform properties, controlling update timing, and managing how invalid tracking data is handled.

| **Property**  | **Description** |
|-------------|-----------|
|[`Tracking Type`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_trackingType)|Specify which transform properties (position, rotation, or both) to update based on the tracked data.|
|[`Update Type`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_updateType)|Determine when updates to the transform occur within Unity's event loop, such as during rendering or gameplay.|
|[`Ignore Tracking State`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_ignoreTrackingState)| Enable to ignore the tracking state and assume that the input pose is valid, even when flagged otherwise.|
|[`Position Input`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_positionInput)|Set an input action that retrieves the position data (Vector3) of the tracked device.|
|[`Rotation Input`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_rotationInput)| Set an input action that retrieves the rotation data (Quaternion) of the tracked device.|
|[`Tracking State Input`](../api/UnityEngine.InputSystem.XR.TrackedPoseDriver.html#UnityEngine_InputSystem_XR_TrackedPoseDriver_trackingStateInput)|Set an input action that determines whether the tracking state (position or rotation) is valid (integer).|

## Tracked Device Raycaster

Use the Tracked Device Raycaster component to enable ray casting from tracked input devices, such as XR controllers, to interact with UI elements rendered in 3D space.

Tracked Device Raycaster works with the the __Canvas__ component, and replaces the standard __GraphicRaycaster__ for XR and AR use cases. Use Tracked Device Raycaster to process pointer events that are based on ray intersections with graphics in world space.

The component also supports occlusion checks, custom ray distance limits, and filtering of reversed graphics to enable UI interactions in immersive environments. You can use it to build XR-enabled user interfaces where traditional 2D input methods, such as a mouse, do not apply.

| **Property**  | **Description** |
|-------------|-----------|
|[`Ignore Reversed Graphics`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_ignoreReversedGraphics)| Enable to ignore graphics whose normal faces away from the direction of the ray.|
|[`Check For 2D Occlusion`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_checkFor2DOcclusion)|Enable occlusion checks for 2D objects, such as sprites in the scene.|
|[`Check For 3D Occulusion`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_checkFor3DOcclusion)|Enable occlusion checks for 3D objects to prevent rays from passing through physical geometry.|
|[`Max Distance`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_maxDistance)|Set the maximum ray distance for interaction detection in world space coordinates.|
|[`Blocking Mask`](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html#UnityEngine_InputSystem_UI_TrackedDeviceRaycaster_blockingMask)|Define the layer mask used to check for occlusion when ray casting.|
