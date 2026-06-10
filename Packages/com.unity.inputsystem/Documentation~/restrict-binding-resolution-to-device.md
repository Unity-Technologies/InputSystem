---
uid:  input-system-restrict-binding-resolution
---

# Restrict binding resolution to a specific device

By default, actions [resolve their bindings](./binding-resolution.md) against all devices present that the Input System is aware of (that is, those listed in [`InputSystem.devices`](xref:UnityEngine.InputSystem.InputSystem)). For example, if there are two gamepads connected, a binding to `<Gamepad>/buttonSouth` picks up both gamepads and allows the action to be performed from either gamepad.

You can override this behavior by restricting an [action asset](./action-assets.md) or individual [action maps](./create-edit-delete-action-maps.md) to a specific set of Devices. If you do this, binding resolution only takes the controls of the specified devices into account.

To restrict an action map to just the first gamepad:

1. Set the `.devices` property of the action map to an array containing a reference to the first item in the `Gamepad.all` array. For example:<br/><br/> `actionMap.devices = new[] { Gamepad.all[0] };`


> [!NOTE]
>  The Input System's [user management](user-management.md) feature and [Player Input component](player-input-component.md) make use of this automatically. They set the [`InputActionMap.devices`](xref:UnityEngine.InputSystem.InputActionMap) for each player automatically, based on the device that is paired to each user.
