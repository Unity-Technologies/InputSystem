---
uid: input-system-sync-device
---

# Sync a device 

A Device may be requested to send an event with its current state through [`RequestSyncCommand`](../api/UnityEngine.InputSystem.LowLevel.RequestSyncCommand.html). It depends on the platform and type of Device whether this is supported or not.

A synchronization request can be explicitly sent using [`InputSystem.TrySyncDevice`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_TrySyncDevice_UnityEngine_InputSystem_InputDevice_). If the device supports sync requests, the method returns true and an [`InputEvent`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html) will have been queued on the device for processing in the next [update](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_Update_).

Synchronization requests are also automatically sent by the Input System in certain situations. See [Background and focus change behavior](#background-and-focus-change-behavior) for more details.
