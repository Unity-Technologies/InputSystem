
# Binding resolution

When the Input System accesses the [Controls](Controls.md) bound to an Action for the first time, the Action resolves its Bindings to match them to existing Controls on existing Devices. In this process, the Action calls [`InputSystem.FindControls<>()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_FindControls__1_System_String_UnityEngine_InputSystem_InputControlList___0___) (filtering for devices assigned to the InputActionMap, if there are any) for the Binding path of each of the Action's bindings. This creates a list of resolved Controls that are now bound to the Action.

Note that a single [Binding path](Controls.md#control-paths) can match multiple Controls:

* A specific Device path such as `<DualShockGamepad>/buttonEast` matches the "Circle" button on a [PlayStation controller](Gamepad.md#playstation-controllers). If you have multiple PlayStation controllers connected, it resolves to the "Circle" button on each of these controllers.

* An abstract Device path such as `<Gamepad>/buttonEast` matches the right action button on any connected gamepad. If you have a PlayStation controller and an [Xbox controller](Gamepad.md#xbox-controllers) connected, it resolves to the "Circle" button on the PlayStation controller, and to the "B" button on the Xbox controller.

* A Binding path can also contain wildcards, such as `<Gamepad>/button*`. This matches any Control on any gamepad with a name starting with "button", which matches all the four action buttons on any connected gamepad. A different example: `*/{Submit}` matches any Control tagged with the "Submit" [usage](Controls.md#control-usages) on any Device.

If there are multiple Bindings on the same Action that all reference the same Control(s), the Control will effectively feed into the Action multiple times. This is to allow, for example, a single Control to produce different input on the same Action by virtue of being bound in a different fashion (composites, processors, interactions, etc). However, regardless of how many times a Control is bound on any given action, it will only be mentioned once in the Action's [array of `controls`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_controls).

To query the Controls that an Action resolves to, you can use [`InputAction.controls`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_controls). You can also run this query if the Action is disabled.

To be notified when binding resolution happens, you can listen to [`InputSystem.onActionChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onActionChange) which triggers [`InputActionChange.BoundControlsAboutToChange`](../api/UnityEngine.InputSystem.InputActionChange.html#UnityEngine_InputSystem_InputActionChange_BoundControlsAboutToChange) before modifying Control lists and triggers [`InputActionChange.BoundControlsChanged`](../api/UnityEngine.InputSystem.InputActionChange.html#UnityEngine_InputSystem_InputActionChange_BoundControlsChanged) after having updated them.

## Binding resolution while Actions are enabled

In certain situations, the [Controls](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_controls) bound to an Action have to be updated more than once. For example, if a new [Device](Devices.md) becomes usable with an Action, the Action may now pick up input from additional controls. Also, if Bindings are added, removed, or modified, Control lists will need to be updated.

This updating of Controls usually happens transparently in the background. However, when an Action is [enabled](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_enabled) and especially when it is [in progress](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_IsInProgress_), there may be a noticeable effect on the Action.

Adding or removing a device &ndash; either [globally](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_devices) or to/from the [device list](../api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_devices) of an Action &ndash; will remain transparent __except__ if an Action is in progress and it is the device of its [active Control](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_activeControl) that is being removed. In this case, the Action will automatically be [cancelled](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled).

Modifying the [binding mask](../api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_bindingMask) or modifying any of the Bindings (such as through [rebinding](#interactive-rebinding) or by adding or removing bindings) will, however, lead to all enabled Actions being temporarily disabled and then re-enabled and resumed.

## Choosing which Devices to use

>__Note__: [`InputUser`](UserManagement.md) and [`PlayerInput`](PlayerInput.md) make use of this facility automatically. They set [`InputActionMap.devices`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_devices) automatically based on the Devices that are paired to the user.

By default, Actions resolve their Bindings against all Devices present in the Input System (that is, [`InputSystem.devices`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_devices)). For example, if there are two gamepads present in the system, a Binding to `<Gamepad>/buttonSouth` picks up both gamepads and allows the Action to be used from either.

You can override this behavior by restricting [`InputActionAssets`](../api/UnityEngine.InputSystem.InputActionAsset.html) or individual [`InputActionMaps`](../api/UnityEngine.InputSystem.InputActionMap.html) to a specific set of Devices. If you do this, Binding resolution only takes the Controls of the given Devices into account.

```
    var actionMap = new InputActionMap();

    // Restrict the action map to just the first gamepad.
    actionMap.devices = new[] { Gamepad.all[0] };
```
