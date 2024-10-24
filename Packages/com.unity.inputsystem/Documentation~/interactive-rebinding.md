
## Interactive rebinding

>__Note:__ To download a sample project which demonstrates how to set up a rebinding user interface with Input System APIs, open the Package Manager, select the Input System Package, and choose the sample project "Rebinding UI" to download.

Runtime rebinding allows users of your application to set their own Bindings.

To allow users to choose their own Bindings interactively, use the  [`InputActionRebindingExtensions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) class. Call the [`PerformInteractiveRebinding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_PerformInteractiveRebinding_UnityEngine_InputSystem_InputAction_System_Int32_) method on an Action to create a rebinding operation. This operation waits for the Input System to register any input from any Device which matches the Action's expected Control type, then uses [`InputBinding.overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath) to assign the Control path for that Control to the Action's Bindings. If the user actuates multiple Controls, the rebinding operation chooses the Control with the highest [magnitude](Controls.md#control-actuation).

>IMPORTANT: You must dispose of [`InputActionRebindingExtensions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) instances via `Dispose()`, so that they don't leak memory on the unmanaged memory heap.

```C#
    void RemapButtonClicked(InputAction actionToRebind)
    {
        var rebindOperation = actionToRebind
            .PerformInteractiveRebinding().Start();
    }
```

The [`InputActionRebindingExtensions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) API is highly configurable to match your needs. For example, you can:

* Choose expected Control types ([`WithExpectedControlType()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithExpectedControlType_System_Type_)).

* Exclude certain Controls ([`WithControlsExcluding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithControlsExcluding_System_String_)).

* Set a Control to cancel the operation ([`WithCancelingThrough()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithCancelingThrough_UnityEngine_InputSystem_InputControl_)).

* Choose which Bindings to apply the operation on if the Action has multiple Bindings ([`WithTargetBinding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithTargetBinding_System_Int32_), [`WithBindingGroup()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithBindingGroup_System_String_), [`WithBindingMask()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithBindingMask_System_Nullable_UnityEngine_InputSystem_InputBinding__)).

Refer to the scripting API reference for [`InputActionRebindingExtensions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) for a full overview.

Note that [`PerformInteractiveRebinding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_PerformInteractiveRebinding_UnityEngine_InputSystem_InputAction_System_Int32_) automatically applies a set of default configurations based on the given action and targeted binding.
