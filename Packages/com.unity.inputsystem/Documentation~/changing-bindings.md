
## Changing Bindings

In general, you can change existing bindings via the [`InputActionSetupExtensions.ChangeBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.html#UnityEngine_InputSystem_InputActionSetupExtensions_ChangeBinding_UnityEngine_InputSystem_InputAction_System_Int32_) method. This returns an accessor that can be used to modify the properties of the targeted [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html). Note that most of the write operations of the accessor are destructive. For non-destructive changes to bindings, see [Applying Overrides](#applying-overrides).

```CSharp
// Get write access to the second binding of the 'fire' action.
var accessor = playerInput.actions['fire'].ChangeBinding(1);

// You can also gain access through the InputActionMap. Each
// map contains an array of all its bindings (see InputActionMap.bindings).
// Here we gain access to the third binding in the map.
accessor = playerInput.actions.FindActionMap("gameplay").ChangeBinding(2);
```

You can use the resulting accessor to modify properties through methods such as [`WithPath`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html#UnityEngine_InputSystem_InputActionSetupExtensions_BindingSyntax_WithPath_) or [`WithProcessors`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html#UnityEngine_InputSystem_InputActionSetupExtensions_BindingSyntax_WithProcessors_).

```CSharp
playerInput.actions["fire"].ChangeBinding(1)
    // Change path to space key.
    .WithPath("<Keyboard>/space");
```

You can also use the accessor to iterate through bindings using [`PreviousBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html#UnityEngine_InputSystem_InputActionSetupExtensions_BindingSyntax_PreviousBinding_) and [`NextBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html#UnityEngine_InputSystem_InputActionSetupExtensions_BindingSyntax_NextBinding_).

```CSharp
// Move accessor to previous binding.
accessor = accessor.PreviousBinding();

// Move accessor to next binding.
accessor = accessor.NextBinding();
```

If the given binding is a [composite](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isComposite), you can address it by its name rather than by index.

```CSharp
// Change the 2DVector composite of the "move" action.
playerInput.actions["move"].ChangeCompositeBinding("2DVector")


//
playerInput.actions["move"].ChangeBinding("WASD")
```
