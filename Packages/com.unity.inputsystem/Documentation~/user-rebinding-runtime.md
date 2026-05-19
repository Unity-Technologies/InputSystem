---
uid: input-system-user-rebinding-runtime
---

# User rebinding at runtime

A common requirement in games is to allow your users to rebind the controls to a configuration of their preference. For example, choosing which button on their controller maps to particular actions in the game. Learn how to implement user rebinding in this section.

| **Topic** | **Description** |
| :--- | :---|
| **[Look up bindings](look-up-bindings.md)** | Retrieve the bindings of an action using its `InputAction.bindings`. |
| **[Display bindings](display-bindings.md)** | Use `InputBinding.effectivePath` to get the currently active path for a binding. |
| **[Rebind an action at runtime](rebind-action-runtime.md)** | Allow users of your application to set their own bindings. |
| **[Save and load rebinds](save-load-rebinds.md)** | Serialize override properties of bindings as JSON strings. |
| **[Restore original bindings](restore-original-bindings.md)** | Remove binding overrides to restore defaults. |

## Additional resources

- [Bindings](bindings.md)
- [Configure actions](configure-actions.md)
- [Setting up input](setting-up-input.md)
- [Input for user interfaces](ui-input.md)
