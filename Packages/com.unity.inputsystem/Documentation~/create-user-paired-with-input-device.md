---
uid: input-system-user-paired-with-device
---

# Create a user paired with an input device 

You can use the [`InputUser.PerformPairingWithDevice`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_PerformPairingWithDevice_UnityEngine_InputSystem_InputDevice_UnityEngine_InputSystem_Users_InputUser_UnityEngine_InputSystem_Users_InputUserPairingOptions_) method to create a new [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) instance and pair it with an [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html). You can also optionally pass in an existing [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) instance to pair it with the Device, if you don't want to create a new user instance.

To query the Devices paired to a specific [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html), use [`InputUser.pairedDevices`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_pairedDevices). To remove the pairing, use [`InputUser.UnpairDevice`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_UnpairDevice_UnityEngine_InputSystem_InputDevice_) or [`InputUser.UnpairDevices`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_UnpairDevices).

## Initial engagement

After you create a user, you can use [`InputUser.AssociateActionsWithUser`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_AssociateActionsWithUser_UnityEngine_InputSystem_IInputActionCollection_) to associate [Input Actions](actions.md) to it, and use [`InputUser.ActivateControlScheme`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_ActivateControlScheme_System_String_) to associate and activate a [Control Scheme](ActionBindings.md#control-schemes). You can use [`InputControlScheme.FindControlSchemeForDevice`](../api/UnityEngine.InputSystem.InputControlScheme.html#UnityEngine_InputSystem_InputControlScheme_FindControlSchemeForDevice__1_UnityEngine_InputSystem_InputDevice___0_) to pick a control scheme that matches the selected Actions and Device:

```
var scheme = InputControlScheme.FindControlSchemeForDevice(user.pairedDevices[0], user.actions.controlsSchemes);
if (scheme != null)
    user.ActivateControlScheme(scheme);
```

When you activate a Control Scheme, the Input System automatically switches the active Binding mask for the user's Actions to that Control Scheme.
