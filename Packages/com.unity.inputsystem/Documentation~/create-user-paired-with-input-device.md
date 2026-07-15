---
uid: input-system-user-paired-with-device
---

# Create a user paired with an input device

You can use the [`InputUser.PerformPairingWithDevice`](xref:UnityEngine.InputSystem.Users.InputUser) method to create a new [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) instance and pair it with an [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice). You can also optionally pass in an existing [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser) instance to pair it with the Device, if you don't want to create a new user instance.

To query the Devices paired to a specific [`InputUser`](xref:UnityEngine.InputSystem.Users.InputUser), use [`InputUser.pairedDevices`](xref:UnityEngine.InputSystem.Users.InputUser). To remove the pairing, use [`InputUser.UnpairDevice`](xref:UnityEngine.InputSystem.Users.InputUser) or [`InputUser.UnpairDevices`](xref:UnityEngine.InputSystem.Users.InputUser).

## Initial engagement

After you create a user, you can use [`InputUser.AssociateActionsWithUser`](xref:UnityEngine.InputSystem.Users.InputUser) to associate [Input Actions](actions.md) to it, and use [`InputUser.ActivateControlScheme`](xref:UnityEngine.InputSystem.Users.InputUser) to associate and activate a [Control Scheme](control-schemes.md). You can use [`InputControlScheme.FindControlSchemeForDevice`](xref:UnityEngine.InputSystem.InputControlScheme) to pick a control scheme that matches the selected Actions and Device:

```
var scheme = InputControlScheme.FindControlSchemeForDevice(user.pairedDevices[0], user.actions.controlsSchemes);
if (scheme != null)
    user.ActivateControlScheme(scheme);
```

When you activate a Control Scheme, the Input System automatically switches the active binding mask for the user's Actions to that Control Scheme.
