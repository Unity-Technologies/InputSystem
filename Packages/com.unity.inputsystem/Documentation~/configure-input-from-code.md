---
uid: input-system-input-from-code
---

# Configure input from code

You can set up [actions](actions.md), [bindings](introduction-to-bindings.md), and related settings in the **Input Actions** editor or with code. Use the topics in this section when you want full control in script, need to generate or load definitions at runtime, or prefer not to rely on a dedicated [Input Action asset](action-assets.md).

| **Topic** | **Description** |
| --- | --- |
| **[Declare stand-alone actions](declare-standalone-actions.md)** | Expose `InputAction` and `InputActionMap` fields on a `MonoBehaviour` and configure them in the **Inspector** window or in code. |
| **[Configure input from JSON](configure-input-from-json.md)** | Create or load `InputActionMap` and `InputActionAsset` instances from JSON strings at edit time or runtime. |
| **[Create actions in code](create-actions-in-code.md)** | Create and configure actions entirely in code. |
| **[Configure bindings from code](configure-bindings-from-code.md)** | Work with `InputBinding` in code: add or remove bindings, composites, parameters, overrides, and control schemes. |

For a high-level map of the actions API (enabling, polling, callbacks), refer to [Scripting with actions API overview](api-overview.md).

## Additional resources

* [Setting up input](setting-up-input.md)
* [Actions](actions.md)
* [Introduction to bindings](introduction-to-bindings.md)
