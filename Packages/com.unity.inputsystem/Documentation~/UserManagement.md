# User Management

The input system supports multi-user management through. This comprises both user account management features on platforms that have respective capabilities built into them (such as Xbox and PS4) as well as features to manage device allocations to one or more local users.

>NOTE: The user management API is quite low-level in nature. If the stock functionality provided by [`PlayerInputManager`](Components.md#playerinputmanager-component) (see [Components](./Components.md)) is sufficient, this provides an easier way to set up user management. The API described here is mainly for when more control over user management is desired.

In the input system, a "User" is represented by the [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) class. Each [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) represents a human interacting with the application. You can have multiple users playing a game together on a single computer/application instance (local multiplayer). Each user has one or more [paired input devices](#device-pairing). A user may be associated with a platform [user account](#user-account-management), if supported by the platform and the devices used.

The [`PlayerInputManager`](Components.md#playerinputmanager-component) class is using [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) for it's user handling internally.

## Device Pairing

You can use the [`InputUser.PerformPairingWithDevice`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_PerformPairingWithDevice_UnityEngine_InputSystem_InputDevice_UnityEngine_InputSystem_Users_InputUser_UnityEngine_InputSystem_Users_InputUserPairingOptions_) method to create a new [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) instance and pair it with an [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) (you can also optionally pass in an existing [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) instance to pair it with the device).

You can query the devices paired to a specific [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) using [`InputUser.pairedDevices`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_pairedDevices), and you can use [`InputUser.UnpairDevice`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_UnpairDevice_UnityEngine_InputSystem_InputDevice_) or [`InputUser.UnpairDevices`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_UnpairDevices) to remove the pairing.

### Initial Engagement

Once you have created a user, you can associate input [actions](Actions.md) to it using [`InputUser.AssociateActionsWithUser`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_AssociateActionsWithUser_UnityEngine_InputSystem_IInputActionCollection_), and pick a [control scheme](ActionBindings.md#control-schemes) to use using [`InputUser.ActivateControlScheme`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_ActivateControlScheme_System_String_). You can use [`InputControlScheme.FindControlSchemeForDevice`](../api/UnityEngine.InputSystem.InputControlScheme.html#UnityEngine_InputSystem_InputControlScheme_FindControlSchemeForDevice__1_UnityEngine_InputSystem_InputDevice___0_) to pick a control scheme matching the selected actions and device:

```
var scheme = InputControlScheme.FindControlSchemeForDevice(user.pairedDevices[0], user.actions.controlsSchemes);
if (scheme != null)
    user.ActivateControlScheme(scheme);
```

Activating a control scheme will automatically switch the active binding mask for the user's actions to the control scheme.

### Loss of Device

If input devices which are paired to a user get disconnected during the session, the [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) class will be notified of this. It will still keep track of the device, and automatically re-pair the device if it becomes available again.

You can get notified of any such changes by subscribing to the [`InputUser.onChange`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_onChange) event.

## User Account Management

A user may be associated with a platform user account, if supported by the platform and the devices used. Support for this is commonly found on consoles. You can query the associated user account for an [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) using the [`platformUserAccountHandle`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_platformUserAccountHandle) property. This property gets determined when the user is first [paired to a device](#device-pairing), and the device has any platform user information the input system can query.

Note that the account associated with an InputUser may change if the player uses the system's facilities to switch to a different account ([`InputUser.onChange`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_onChange) will receive an `InputUserChange.AccountChanged` notification).

Platforms that support user account association are Xbox One, PS4, Switch and UWP. Note that for WSA/UWP apps, the "User Account Information" capability must be enabled for the app in order for user information to come through on input devices.

## Debugging

Check the debugger docs to learn [how to debug active users](Debugging.md#debugging-usersplayerinput).
