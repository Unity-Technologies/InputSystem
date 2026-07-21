---
uid: input-system-save-load-rebinds
---

# Save and load rebinds

You can serialize override properties of [Bindings](xref:UnityEngine.InputSystem.InputBinding) by serializing them as JSON strings and restoring them from these. Use [`SaveBindingOverridesAsJson`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) to create these strings and [`LoadBindingOverridesFromJson`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) to restore overrides from them.

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
