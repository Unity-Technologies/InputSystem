---
uid: input-system-sync-device
---

# Sync a device

A Device may be requested to send an event with its current state through [`RequestSyncCommand`](xref:UnityEngine.InputSystem.LowLevel.RequestSyncCommand). It depends on the platform and type of Device whether this is supported or not.

A synchronization request can be explicitly sent using [`InputSystem.TrySyncDevice`](xref:UnityEngine.InputSystem.InputSystem). If the device supports sync requests, the method returns true and an [`InputEvent`](xref:UnityEngine.InputSystem.LowLevel.InputEvent) will have been queued on the device for processing in the next [update](xref:UnityEngine.InputSystem.InputSystem).

Synchronization requests are also automatically sent by the Input System in certain situations. Refer to [Background and focus change behavior](device-background-focus-changes.md) for more details.
