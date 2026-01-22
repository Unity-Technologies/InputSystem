---
uid: input-system-layout-inheritance
---

# Layout inheritance 

You can derive a layout from an existing layout. This process is based on merging the information from the derived layout on top of the information that the base layout contains.

* For layouts defined as types, the base layout is the layout of the base type (if any).
* For layouts defined in JSON, you can specify the base layout in the `extends` property of the root node.
* For layouts created in code using [`InputControlLayout.Builder`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.html), you can specify a base layout using [`InputControlLayout.Builder.Extend()`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.html#UnityEngine_InputSystem_Layouts_InputControlLayout_Builder_Extend_System_String_).