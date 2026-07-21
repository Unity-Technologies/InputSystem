---
uid: input-system-user-management-about
---

# About user management

The Input System supports multi-user management through the [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) class. This comprises both user account management features on platforms that have these capabilities built into them (such as Xbox and PS4), as well as features to manage Device allocations to one or more local users.

> [!NOTE]
> The user management API is quite low-level in nature. The stock functionality of Player Input Manager component (refer to [Player Input Manager](player-input-manager-component.md)) provides an easier way to set up user management. The API described here is useful when you want more control over user management.

In the Input System, each [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) represents a human interacting with the application. For example, you can have multiple users playing a game together on a single computer or device (local multiplayer), where each user has one or more [paired Input Devices](create-user-paired-with-input-device.md).

The [`PlayerInputManager`](player-input-manager-component.md) class uses [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) internally to handle users.

> [!NOTE]
> In the editor, all `InputUser` instances are automatically removed when exiting play mode thus also removing any device pairings. In essence, `InputUser` is considered a player-only API.
