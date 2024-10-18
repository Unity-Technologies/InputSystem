## Using Processors

You can install Processors on [bindings](ActionBindings.md), [actions](Actions.md) or on [controls](Controls.md).

Each Processor is [registered](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterProcessor__1_System_String_) using a unique name. To replace an existing Processor, register your own Processor under an existing name.

Processors can have parameters which can be booleans, integers, or floating-point numbers. When created in data such as [bindings](./ActionBindings.md), processors are described as strings that look like function calls:

```CSharp
    // This references the processor registered as "scale" and sets its "factor"
    // parameter (a floating-point value) to a value of 2.5.

    "scale(factor=2.5)"

    // Multiple processors can be chained together. They are processed
    // from left to right.
    //
    // Example: First invert the value, then normalize [0..10] values to [0..1].

    "invert,normalize(min=0,max=10)"
```
