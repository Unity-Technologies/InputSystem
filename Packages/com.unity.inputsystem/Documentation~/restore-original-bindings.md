---
uid: input-system-restore-original-bindings
---

# Restore original bindings

You can remove Binding overrides and thus restore defaults by using [`RemoveBindingOverride`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) or [`RemoveAllBindingOverrides`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions).

```CSharp
// Remove binding overrides from the first binding of the "fire" action.
playerInput.actions["fire"].RemoveBindingOverride(0);

// Remove all binding overrides from the "fire" action.
playerInput.actions["fire"].RemoveAllBindingOverrides();

// Remove all binding overrides from a player's actions.
playerInput.actions.RemoveAllBindingOverrides();
```
