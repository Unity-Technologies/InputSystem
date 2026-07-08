---
uid: input-system-override-layout-definitions
---

# Override layout definitions

You can non-destructively change aspects of an existing layout using layout overrides. You can call [`InputSystem.RegisterLayoutOverride`](xref:UnityEngine.InputSystem.InputSystem) to register a layout as an override of its [base layout](layout-inheritance.md). The system then adds any property present in the override to the base layout or to existing properties.

```CSharp
// Add an extra Control to the "Mouse" layout
const string json = @"
    {
        ""name"" : ""Overrides"",
        ""extend"" : ""Mouse"",
        ""controls"" : [
            { ""name"" : ""extraControl"", ""layout"" : ""Button"" }
        ]
    }
";

InputSystem.RegisterLayoutOverride(json);
```
