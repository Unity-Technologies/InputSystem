---
uid: input-system-intro-processors
---

# Introduction to processors

Input processors apply processing to input values, and return the result. The Input System’s [built-in processors](built-in-processors.md) can apply value clamping, scaling, normalization, inversion, and deadzones. You can also create [custom processors](write-custom-processors.md) to apply additional data processing to input values.

You can install processors on [bindings](ActionBindings.md), [actions](actions.md) or [controls](controls.md). The Input System [registers](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterProcessor__1_System_String_) each processor with a unique name. This means that if you need to replace an existing processor, you need to register the new processor under the name of the existing processor.

Processors can have boolean, integer, and floating-point number parameters. When created in data such as [bindings](./ActionBindings.md), processors are described as strings that look like function calls. 

For example, the following string references the processor registered as "scale" and sets its "factor" parameter to a floating-point value of 2.5:

```CSharp
    "scale(factor=2.5)"
```

Multiple processors can be chained together. The Input System processes them in the order they appear in code. For example, the following string inverts a value, then normalizes [0..10] values to [0..1]:

```CSharp
    "invert,normalize(min=0,max=10)"
```

## Processors on bindings and actions

When you create bindings for your [actions](actions.md), you can choose to add processors to the bindings. These process the values from the controls they bind to, before the system applies them to the action value. For example, you could invert the `Vector2` values from the controls along the Y axis before passing the values to the associated action. 

Processors on actions work the same way, but affect all bindings on an action. 

If there are processors on both the binding and the action, the Input System processes the processors from the binding first.

To apply a processor to a binding or action, refer to [Add processors to bindings and actions](add-processors-bindings-actions.md).

## Processors on controls

You can have any number of processors directly on an [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html), which then process the values read from the Control. Whenever you call [`ReadValue`](../api/UnityEngine.InputSystem.InputControl-1.html#UnityEngine_InputSystem_InputControl_1_ReadValue) on a Control, all processors on that Control process the value before it gets returned to you. You can use [`ReadUnprocessedValue`](../api/UnityEngine.InputSystem.InputControl-1.html#UnityEngine_InputSystem_InputControl_1_ReadUnprocessedValue) on a Control to bypass the processors.

The devices that the Input System supports out of the box already have some useful processors added to their controls by default. For example, sticks on gamepads have a [Stick Deadzone](built-in-processors.md#stick-deadzone) processor.

To apply a processor to a control, refer to [Add processors to controls](add-processors-controls.md)
