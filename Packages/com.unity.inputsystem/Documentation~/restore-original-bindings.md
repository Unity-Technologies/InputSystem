
### Restoring original Bindings

You can remove Binding overrides and thus restore defaults by using [`RemoveBindingOverride`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RemoveBindingOverride_UnityEngine_InputSystem_InputAction_System_Int32_) or [`RemoveAllBindingOverrides`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RemoveAllBindingOverrides_UnityEngine_InputSystem_IInputActionCollection2_).

```CSharp
// Remove binding overrides from the first binding of the "fire" action.
playerInput.actions["fire"].RemoveBindingOverride(0);

// Remove all binding overrides from the "fire" action.
playerInput.actions["fire"].RemoveAllBindingOverrides();

// Remove all binding overrides from a player's actions.
playerInput.actions.RemoveAllBindingOverrides();
```
