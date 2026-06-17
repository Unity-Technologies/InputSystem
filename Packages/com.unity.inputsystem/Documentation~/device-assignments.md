---
uid: input-system-device assignments
---

# Device assignments

If the `PlayerInput` component has any Devices assigned, it matches these to the [Control Schemes](control-schemes.md) in the associated action asset, and only enables Control Schemes which match its Input Devices.

Each `PlayerInput` can have one or more Devices assigned to it. By default, no two `PlayerInput` components are assigned the same Devices, but you can force this; to do so, manually assign Devices to a player when calling [`PlayerInput.Instantiate`](xref:UnityEngine.InputSystem.PlayerInput), or call [`InputUser.PerformPairingWithDevice`](xref:UnityEngine.InputSystem.Users.InputUser) on the `InputUser` of a `PlayerInput`.
