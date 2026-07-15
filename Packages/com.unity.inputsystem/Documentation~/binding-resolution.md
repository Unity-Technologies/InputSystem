---
uid: input-system-binding-resolution
---

# Binding resolution

Binding resolution refers to when the Input System looks up which actual controls on connected devices should be used by each action, based on their binding control paths.

## Why bindings are resolved

Each [simple binding](./binding-types.md) has a [control path](./control-paths.md), which determines which [control](controls.md) (or controls) should be associated with the action. For [composite bindings](./composite-bindings.md), each of the composites sub-bindings (or **parts**) has a control path.

Control paths are stored as a string that describes where to find the relevant control or controls for the binding. For example, a control path "`<Gamepad>/buttonEast`" refers to the right action button on any connected gamepad.

Because control paths can refer to specific devices, or more broadly to a device type, and can also contain wildcard characters, and there may be any combinations of input hardware connected, the Input System must **resolve** the bindings at runtime to work out which controls on connected devices are valid for each action. This occurs when the Input System accesses an action for the first time.

## What happens during resolution

During binding resolution, the action automatically calls [`InputSystem.FindControls<>()`](xref:UnityEngine.InputSystem.InputSystem) (filtering for devices assigned to the InputActionMap, if there are any) for the binding path of each of the Action's bindings. This creates a list of resolved Controls that are now bound to the Action.

Note that a single [binding control path](control-paths.md) can match multiple Controls. For example:

* A device-specific path such as `<DualShockGamepad>/buttonEast` matches the "Circle" button on a [PlayStation controller](gamepads-p.md). If you have multiple PlayStation controllers connected, it resolves to the "Circle" button on each of these controllers.

* An abstract device path such as `<Gamepad>/buttonEast` matches the right action button on any connected gamepad. If you have a PlayStation controller and an [Xbox controller](gamepads-xbox.md) connected, it resolves to the "Circle" button on the PlayStation controller, and to the "B" button on the Xbox controller.

* A binding path can also contain wildcards, such as `<Gamepad>/button*`. This matches any control on any gamepad with a name starting with "button", which matches all the four action buttons on any connected gamepad. A different example: `*/{Submit}` matches any control tagged with the "Submit" [usage](control-usages.md) on any device.

If there are multiple bindings on the same action that all reference the same control(s), the control will effectively feed into the action multiple times. This is to allow, for example, a single control to produce different input on the same action by virtue of being bound in a different fashion ([composites](./composite-bindings.md), [processors](./add-processors-bindings-actions.md), [interactions](Interactions.md), etc). However, regardless of how many times a control is bound on any given action, it will only appear once in the action's [array of `controls`](xref:UnityEngine.InputSystem.InputAction.controls).

To query the Controls that an Action resolves to, you can use [`InputAction.controls`](xref:UnityEngine.InputSystem.InputAction). You can also run this query if the Action is disabled.

To be notified when binding resolution happens, you can listen to [`InputSystem.onActionChange`](xref:UnityEngine.InputSystem.InputSystem.onActionChange) which triggers [`InputActionChange.BoundControlsAboutToChange`](xref:UnityEngine.InputSystem.InputActionChange.BoundControlsAboutToChange) before modifying Control lists and triggers [`InputActionChange.BoundControlsChanged`](xref:UnityEngine.InputSystem.InputActionChange.BoundControlsChanged) after having updated them.

## Binding resolution while actions are enabled

In certain situations, the controls bound to an action have to be updated more than once. For example, if a new device is plugged in and becomes usable with an action, the action may now pick up input from additional controls. Also, if bindings are added, removed, or modified, control lists will need to be updated.

This updating of controls usually happens transparently in the background. However, when an action is [enabled](xref:UnityEngine.InputSystem.InputAction) and especially when it is [in progress](xref:UnityEngine.InputSystem.InputAction), there may be a noticeable effect on the Action.

Adding or removing a device &ndash; either [globally](xref:UnityEngine.InputSystem.InputSystem.devices) or to/from the [device list](xref:UnityEngine.InputSystem.InputActionAsset.devices) of an Action &ndash; will remain transparent __except__ if an Action is in progress and it is the device of its [active Control](xref:UnityEngine.InputSystem.InputAction.activeControl) that is being removed. In this case, the Action will automatically be [cancelled](xref:UnityEngine.InputSystem.InputAction.canceled).

Modifying the [binding mask](xref:UnityEngine.InputSystem.InputActionAsset.bindingMask) or modifying any of the bindings (such as through [rebinding](./interactive-rebinding.md) or by adding or removing bindings) will, however, lead to all enabled actions being temporarily disabled and then re-enabled and resumed.
