
### Apply binding overrides

You can override aspects of any Binding at run-time non-destructively. Specific properties of [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html) have an `override` variant that, if set, will take precedent over the property that they shadow.  All `override` properties are of type `String`.

|Property|Override|Description|
|--------|--------|-----------|
|[`path`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_path)|[`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath)|Replaces the [Control path](./Controls.md#control-paths) that determines which Control(s) are referenced in the binding. If [`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath) is set to an empty string, the binding is effectively disabled.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`processors`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_processors)|[`overrideProcessors`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overrideProcessors)|Replaces the [processors](./Processors.md) applied to the binding.<br><br>Example: `"invert,normalize(min=0,max=10)"`|
|[`interactions`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_interactions)|[`overrideInteractions`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overrideInteractions)|Replaces the [interactions](./Interactions.md) applied to the binding.<br><br>Example: `"tap(duration=0.5)"`|

>NOTE: The `override` property values will not be saved along with the Actions (for example, when calling [`InputActionAsset.ToJson()`](../api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_ToJson)). See [Saving and loading rebinds](#saving-and-loading-rebinds) for details about how to persist user rebinds.

To set the various `override` properties, you can use the [`ApplyBindingOverride`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_ApplyBindingOverride_UnityEngine_InputSystem_InputAction_UnityEngine_InputSystem_InputBinding_) APIs.

```CSharp
// Rebind the "fire" action to the left trigger on the gamepad.
playerInput.actions["fire"].ApplyBindingOverride("<Gamepad>/leftTrigger");
```

In most cases, it is best to locate specific bindings using APIs such as [`GetBindingIndexForControl`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_GetBindingIndexForControl_) and to then apply the override to that specific binding.

```CSharp
// Find the "Jump" binding for the space key.
var jumpAction = playerInput.actions["Jump"];
var bindingIndex = jumpAction.GetBindingIndexForControl(Keyboard.current.spaceKey);

// And change it to the enter key.
jumpAction.ApplyBindingOverride(bindingIndex, "<Keyboard>/enter");
```
