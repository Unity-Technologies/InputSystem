## Looking up Bindings

You can retrieve the bindings of an action using its [`InputAction.bindings`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_bindings) property which returns a read-only array of [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html) structs.

```CSharp
    // Get bindings of "fire" action.
    var fireBindings = playerInput.actions["fire"].bindings;
```

Also, all the bindings for all actions in an [`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html) are made available through the [`InputActionMap.bindings`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindings) property. The bindings are associated with actions through an [action ID](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_id) or [action name](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_name) stored in the [`InputBinding.action`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_action) property.

```CSharp
    // Get all bindings in "gameplay" action map.
    var gameplayBindings = playerInput.actions.FindActionMap("gameplay").bindings;
```

You can also look up specific the indices of specific bindings in [`InputAction.bindings`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_bindings) using the [`InputActionRebindingExtensions.GetBindingIndex`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_GetBindingIndex_UnityEngine_InputSystem_InputAction_UnityEngine_InputSystem_InputBinding_) method.

```CSharp
    // Find the binding in the "Keyboard" control scheme.
    playerInput.actions["fire"].GetBindingIndex(group: "Keyboard");

    // Find the first binding to the space key in the "gameplay" action map.
    playerInput.FindActionMap("gameplay").GetBindingIndex(
        new InputBinding { path = "<Keyboard>/space" });
```

Finally, you can look up the binding that corresponds to a specific control through [`GetBindingIndexForControl`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_GetBindingIndexForControl_). This way, you can, for example, map a control found in the [`controls`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_controls) array of an [`InputAction`](../api/UnityEngine.InputSystem.InputAction.html) back to an [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html).

```CSharp
    // Find the binding that binds LMB to "fire". If there is no such binding,
    // bindingIndex will be -1.
    var fireAction = playerInput.actions["fire"];
    var bindingIndex = fireAction.GetBindingIndexForControl(Mouse.current.leftButton);
    if (binding == -1)
        Debug.Log("Fire is not bound to LMB of the current mouse.");
```
