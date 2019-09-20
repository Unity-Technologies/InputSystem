# User Management

The Input System supports multi-user management through the [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) class. This comprises both user account management features on platforms that have these capabilities built into them (such as Xbox and PS4), as well as features to manage Device allocations to one or more local users.

>__Note__: The user management API is quite low-level in nature. If the stock functionality provided by [`PlayerInputManager`](Components.md#playerinputmanager-component) (see [Components](./Components.md)) is sufficient, this provides an easier way to set up user management. The API described here is useful when you want more control over user management.

In the input system, a user is represented by the [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) class. Each [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) represents a human interacting with the application. You can have multiple users playing a game together on a single computer or device (local multiplayer), where each user has one or more [paired Input Devices](#device-pairing). A user may be associated with a platform [user account](#user-account-management), if supported by the platform and the Devices used.

The [`PlayerInputManager`](Components.md#playerinputmanager-component) class uses [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) internally for it's user handling.

## Device pairing

You can use the [`InputUser.PerformPairingWithDevice`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_PerformPairingWithDevice_UnityEngine_InputSystem_InputDevice_UnityEngine_InputSystem_Users_InputUser_UnityEngine_InputSystem_Users_InputUserPairingOptions_) method to create a new [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) instance and pair it with an [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html). You can also optionally pass in an existing [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) instance to pair it with the Device, if you don't want to create a new user instance.

You can query the Devices paired to a specific [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) using [`InputUser.pairedDevices`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_pairedDevices), and use [`InputUser.UnpairDevice`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_UnpairDevice_UnityEngine_InputSystem_InputDevice_) or [`InputUser.UnpairDevices`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_UnpairDevices) to remove the pairing.

### Initial engagement

Once you have created a user, you can associate [Input Actions](Actions.md) to it using [`InputUser.AssociateActionsWithUser`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_AssociateActionsWithUser_UnityEngine_InputSystem_IInputActionCollection_), and pick a [Control Scheme](ActionBindings.md#control-schemes) to use by using [`InputUser.ActivateControlScheme`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_ActivateControlScheme_System_String_). You can use [`InputControlScheme.FindControlSchemeForDevice`](../api/UnityEngine.InputSystem.InputControlScheme.html#UnityEngine_InputSystem_InputControlScheme_FindControlSchemeForDevice__1_UnityEngine_InputSystem_InputDevice___0_) to pick a control scheme matching the selected Actions and Device:

```
var scheme = InputControlScheme.FindControlSchemeForDevice(user.pairedDevices[0], user.actions.controlsSchemes);
if (scheme != null)
    user.ActivateControlScheme(scheme);
```

Activating a Control Scheme automatically switches the active Binding mask for the user's Actions to the Control Scheme.

### Loss of Device

If Input Devices which are paired to a user get disconnected during the session, the system notifies the [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) class. It still keeps track of the Device, and automatically re-pairs the Device if it becomes available again.

You can get notified of any such changes by subscribing to the [`InputUser.onChange`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_onChange) event.

## User account management

A user can be associated with a platform user account, if the platform and the Devices used both support this. Consoles commonly support this functionality. You can query the associated user account for an [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) using the [`platformUserAccountHandle`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_platformUserAccountHandle) property. This property gets determined when the user is first [paired to a Device](#device-pairing), and the Device has any platform user information the Input System can query.

Note that the account associated with an InputUser may change if the player uses the platform's facilities to switch to a different account ([`InputUser.onChange`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_onChange) receives an `InputUserChange.AccountChanged` notification).

Platforms that support user account association are Xbox One, PlayStation 4, Nintendo Switch, and UWP. Note that for WSA/UWP apps, the *User Account Information* capability must be enabled for the app in order for user information to come through on input devices.

## Debugging

Check the debugger documentation to learn [how to debug active users](Debugging.md#debugging-users-and-playerinput).
