---
uid: input-system-enable-actions
---

# Enabling actions

Actions have an **enabled** state, meaning you can enable or disable them to suit different situations.

## Project-wide actions

If you are using the recommended [project-wide actions](./about-project-wide-actions.md) workflow, those actions are enabled automatically as your project starts. You do not need to enable them.

## Other actions

For actions defined elsewhere, such as in an action asset not assigned as project-wide, or defined your own code, those actions begin in a disabled state, and you must enable them before you can use them to respond to input.

You can enable actions individually, or as a group by enabling the Action Map which contains them.

```CSharp
// Enable a single action.
lookAction.Enable();

// Enable an en entire action map.
gameplayActions.Enable();
```

## Behaviour when enabled

When an action is enabled, the Input System resolves its bindings, unless it has done so already, or if the set of devices that the action can use has not changed. For more details about this process, see the documentation on [binding resolution](binding-resolution.md).

You can't change certain aspects of the configuration, such as an action's bindings, while an action is enabled. To stop actions or action maps from responding to input, call [`Disable`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_Disable).

While enabled, the action actively monitors the [control(s)](controls.md) it is bound to. If a bound control changes state, the action processes the change. If the control's change represents an [interaction](Interactions.md) change, the action creates a response. All of this happens during the Input System update logic. Depending on the [update mode](update-mode.md) selected in the input settings, this happens once every frame, once every fixed update, or manually if the update mode setting is set to manual.




