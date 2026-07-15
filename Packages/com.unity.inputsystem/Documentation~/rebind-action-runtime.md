---
uid: input-system-rebind-at-runtime
---

# Rebind an action at runtime

> [!NOTE]
> To download a sample project which demonstrates how to set up a rebinding user interface with Input System APIs, open the Package Manager, select the Input System Package, and choose the sample project "Rebinding UI" to download.

Runtime rebinding allows users of your application to set their own bindings.

To allow users to choose their own bindings interactively, use the  [`InputActionRebindingExtensions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) class. Call the [`PerformInteractiveRebinding()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) method on an Action to create a rebinding operation. This operation waits for the Input System to register any input from any Device which matches the Action's expected Control type, then uses [`InputBinding.overridePath`](xref:UnityEngine.InputSystem.InputBinding) to assign the Control path for that Control to the Action's bindings. If the user actuates multiple Controls, the rebinding operation chooses the Control with the highest [magnitude](control-actuation.md).

> [!IMPORTANT]
> You must dispose of [`InputActionRebindingExtensions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) instances with `Dispose()`, so that they don't leak memory on the unmanaged memory heap.

```C#
    void RemapButtonClicked(InputAction actionToRebind)
    {
        var rebindOperation = actionToRebind
            .PerformInteractiveRebinding().Start();
    }
```

The [`InputActionRebindingExtensions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) API is highly configurable to match your needs. For example, you can:

* Choose expected Control types ([`WithExpectedControlType()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation)).

* Exclude certain Controls ([`WithControlsExcluding()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation)).

* Set a Control to cancel the operation ([`WithCancelingThrough()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation)).

* Choose which bindings to apply the operation on if the Action has multiple bindings ([`WithTargetBinding()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation), [`WithBindingGroup()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation), [`WithBindingMask()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation)).

Refer to the scripting API reference for [`InputActionRebindingExtensions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) for a full overview.

Note that [`PerformInteractiveRebinding()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) automatically applies a set of default configurations based on the given action and targeted binding.
