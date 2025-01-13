
## Saving and loading rebinds

You can serialize override properties of [Bindings](../api/UnityEngine.InputSystem.InputBinding.html) by serializing them as JSON strings and restoring them from these. Use [`SaveBindingOverridesAsJson`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_SaveBindingOverridesAsJson_UnityEngine_InputSystem_IInputActionCollection2_) to create these strings and [`LoadBindingOverridesFromJson`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_LoadBindingOverridesFromJson_UnityEngine_InputSystem_IInputActionCollection2_System_String_System_Boolean_) to restore overrides from them.

```CSharp
// Store player rebinds in PlayerPrefs.
var rebinds = playerInput.actions.SaveBindingOverridesAsJson();
PlayerPrefs.SetString("rebinds", rebinds);

// Restore player rebinds from PlayerPrefs (removes all existing
// overrides on the actions; pass `false` for second argument
// in case you want to prevent that).
var rebinds = PlayerPrefs.GetString("rebinds");
playerInput.actions.LoadBindingOverridesFromJson(rebinds);
```
