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

You can't change certain aspects of the configuration, such as an action's bindings, while an action is enabled. To stop actions or action maps from responding to input, call [`Disable`](xref:UnityEngine.InputSystem.InputAction).

While enabled, the action actively monitors the [control(s)](controls.md) it is bound to. If a bound control changes state, the action processes the change. If the control's change represents an [interaction](Interactions.md) change, the action creates a response. All of this happens during the Input System update logic. Depending on the [update mode](update-mode.md) selected in the input settings, this happens once every frame, once every fixed update, or manually if the update mode setting is set to manual.

## Overlapping bindings and action priority

When several enabled actions share the same physical control (for example a plain **B** key action and a **Shift**+**B** composite), the Input System can resolve which action should respond first using either complexity-based or priority-based resolution. Both modes are configured in **Project Settings** > **Input System Package** under [Improved Shortcut Support](xref:input-system-settings).

Each action has a [`Priority`](xref:UnityEngine.InputSystem.InputAction.Priority) property. The range is from `0` to `65535`, and is clamped when set. A higher value means a higher priority, notified first.

The `Priority` value applies to all bindings on that action. Serialized priority is always stored on the asset; at runtime it's used only when **Action Priority Shortcut Resolution** is enabled. In that case, the **Priority** field is also shown in the [Input Actions Editor](xref:input-system-input-actions-editor-landing).

When action priority resolution is active:

- Higher priority actions are notified before lower-priority actions on the same control.
- When an action reaches the Performed phase, priority `0` doesn't mark the input event as handled, so lower-priority actions in the same overlap group can still respond on that event.
- Any priority greater than zero can mark the event as handled and suppress strictly lower-priority actions in the same group for that event.
- Actions with the same priority are not suppressed relative to each other; both can perform in the same update if their bindings fire.

To set priority in code, use `.Priority`:

```CSharp
fireAction.Priority = 10;
reloadAction.Priority = 5;
```

You can also edit the **Priority** field on an action in the Input Actions Editor when **Action Priority Shortcut Resolution** is enabled.

For information about composite shortcuts and complexity ordering, refer to [Multiple input sequences (such as keyboard shortcuts)](xref:input-system-binding-conflicts#multiple-input-sequences-such-as-keyboard-shortcuts).
