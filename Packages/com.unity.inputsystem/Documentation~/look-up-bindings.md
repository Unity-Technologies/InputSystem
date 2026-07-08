---
uid: input-system-look-up-bindings
---

# Look up bindings

You can retrieve the bindings of an action using its [`InputAction.bindings`](xref:UnityEngine.InputSystem.InputAction) property which returns a read-only array of [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding) structs.

```CSharp
    // Get bindings of "fire" action.
    var fireBindings = playerInput.actions["fire"].bindings;
```

Also, all the bindings for all actions in an [`InputActionMap`](xref:UnityEngine.InputSystem.InputActionMap) are made available through the [`InputActionMap.bindings`](xref:UnityEngine.InputSystem.InputActionMap) property. The bindings are associated with actions through an [action ID](xref:UnityEngine.InputSystem.InputAction) or [action name](xref:UnityEngine.InputSystem.InputAction) stored in the [`InputBinding.action`](xref:UnityEngine.InputSystem.InputBinding) property.

```CSharp
    // Get all bindings in "gameplay" action map.
    var gameplayBindings = playerInput.actions.FindActionMap("gameplay").bindings;
```

You can also look up the indices of specific bindings in [`InputAction.bindings`](xref:UnityEngine.InputSystem.InputAction) using the [`InputActionRebindingExtensions.GetBindingIndex`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) method.

```CSharp
    // Find the binding in the "Keyboard" control scheme.
    playerInput.actions["fire"].GetBindingIndex(group: "Keyboard");

    // Find the first binding to the space key in the "gameplay" action map.
    playerInput.FindActionMap("gameplay").GetBindingIndex(
        new InputBinding { path = "<Keyboard>/space" });
```

Finally, you can look up the binding that corresponds to a specific control through [`GetBindingIndexForControl`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions). This way, you can, for example, map a control found in the [`controls`](xref:UnityEngine.InputSystem.InputAction) array of an [`InputAction`](xref:UnityEngine.InputSystem.InputAction) back to an [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding).

```CSharp
    // Find the binding that binds LMB to "fire". If there is no such binding,
    // bindingIndex will be -1.
    var fireAction = playerInput.actions["fire"];
    var bindingIndex = fireAction.GetBindingIndexForControl(Mouse.current.leftButton);
    if (binding == -1)
        Debug.Log("Fire is not bound to LMB of the current mouse.");
```
