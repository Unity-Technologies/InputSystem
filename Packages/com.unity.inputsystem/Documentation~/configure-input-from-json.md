---
uid: input-system-configure-input-from-json
---

# Configure input from JSON

You can load actions as JSON in the form of a set of action maps or as a full [`InputActionAsset`](xref:UnityEngine.InputSystem.InputActionAsset). This also works at runtime in the Player.

```CSharp
// Load a set of action maps from JSON.
var maps = InputActionMap.FromJson(json);

// Load an entire InputActionAsset from JSON.
var asset = InputActionAsset.FromJson(json);
```
