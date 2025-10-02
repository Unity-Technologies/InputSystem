---
uid: input-system-user-management
---
# User Management

The Input System supports multi-user management through the [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) class. This comprises both user account management features on platforms that have these capabilities built into them (such as Xbox and PS4), as well as features to manage Device allocations to one or more local users.

> [!NOTE]
> The user management API is quite low-level in nature. The stock functionality of Player Input Manager component (see [Player Input Manager](xref:input-system-player-input-manager)) provides an easier way to set up user management. The API described here is useful when you want more control over user management.

In the Input System, each [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) represents a human interacting with the application. For example, you can have multiple users playing a game together on a single computer or device (local multiplayer), where each user has one or more [paired Input Devices](#device-pairing).

The [`PlayerInputManager`](xref:input-system-player-input-manager) class uses [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) internally to handle users.

> [!NOTE]
> In the editor, all `InputUser` instances are automatically removed when exiting play mode thus also removing any device pairings. In essence, `InputUser` is considered a player-only API.

## Device pairing

You can use the [`InputUser.PerformPairingWithDevice`](xref:UnityEngine.InputSystem.Users.InputUser.PerformPairingWithDevice(UnityEngine.InputSystem.InputDevice,UnityEngine.InputSystem.Users.InputUser,UnityEngine.InputSystem.Users.InputUserPairingOptions)) method to create a new [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) instance and pair it with an [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice). You can also optionally pass in an existing [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) instance to pair it with the Device, if you don't want to create a new user instance.

To query the Devices paired to a specific [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser), use [`InputUser.pairedDevices`](xref:UnityEngine.InputSystem.Users.InputUser.pairedDevices). To remove the pairing, use [`InputUser.UnpairDevice`](xref:UnityEngine.InputSystem.Users.InputUser.UnpairDevice(UnityEngine.InputSystem.InputDevice)) or [`InputUser.UnpairDevices`](xref:UnityEngine.InputSystem.Users.InputUser.UnpairDevices).

### Initial engagement

After you create a user, you can use [`InputUser.AssociateActionsWithUser`](xref:UnityEngine.InputSystem.Users.InputUser.AssociateActionsWithUser(UnityEngine.InputSystem.IInputActionCollection)) to associate [Input Actions](xref:input-system-actions) to it, and use [`InputUser.ActivateControlScheme`](xref:UnityEngine.InputSystem.Users.InputUser.ActivateControlScheme(System.String)) to associate and activate a [Control Scheme](xref:input-system-action-bindings#control-schemes). You can use [`InputControlScheme.FindControlSchemeForDevice`](xref:UnityEngine.InputSystem.InputControlScheme.FindControlSchemeForDevice``1(UnityEngine.InputSystem.InputDevice,``0)) to pick a control scheme that matches the selected Actions and Device:

```
var scheme = InputControlScheme.FindControlSchemeForDevice(user.pairedDevices[0], user.actions.controlsSchemes);
if (scheme != null)
    user.ActivateControlScheme(scheme);
```

When you activate a Control Scheme, the Input System automatically switches the active Binding mask for the user's Actions to that Control Scheme.

### Loss of Device

If paired Input Devices disconnect during the session, the system notifies the [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) class. It still keeps track of the Device, and automatically re-pairs the Device if it becomes available again.

To get notifications about these changes, subscribe to the [`InputUser.onChange`](xref:UnityEngine.InputSystem.Users.InputUser.onChange) event.

## Debugging

Check the debugger documentation to learn [how to debug active users](xref:input-system-debugging#debugging-users-and-playerinput).
