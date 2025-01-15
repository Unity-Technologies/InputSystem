# Restrict bindings to specific devices

By default, Actions resolve their Bindings against all Devices present in the Input System (that is, [`InputSystem.devices`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_devices)). For example, if there are two gamepads present in the system, a Binding to `<Gamepad>/buttonSouth` picks up both gamepads and allows the Action to be used from either.

You can override this behavior by restricting [`InputActionAssets`](../api/UnityEngine.InputSystem.InputActionAsset.html) or individual [`InputActionMaps`](../api/UnityEngine.InputSystem.InputActionMap.html) to a specific set of Devices. If you do this, Binding resolution only takes the Controls of the given Devices into account.

>__Note__: [`InputUser`](user-management.md) and [`PlayerInput`](player-input-component.md) make use of this facility automatically. They set [`InputActionMap.devices`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_devices) automatically based on the Devices that are paired to the user.


To restrict an action map to just the first gamepad:

1. Set the `.devices` property of the action map to an array containing a reference to the first item in the `Gamepad.all` array. For example:<br/><br/> `actionMap.devices = new[] { Gamepad.all[0] };`


