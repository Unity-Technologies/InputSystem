---
uid: input-system-custom-processors
---

# Write custom processors

You can write custom processors to use with [bindings](bindings.md), [actions](actions.md) and [controls](controls.md) in your Project. Custom processors are available in the UI and code in the same way as the [built-in processors](built-in-processors.md).

To create a custom processor:

1. [Add a processor class and method](#add-a-processor-class-and-method)
1. [Register the new processor to the Input System](#register-the-new-processor-to-the-input-system)
1. [Customize the Editor UI](#customize-the-editor-ui) for the new processor, if necessary.

## Add a processor class and method

**1.** Add a class derived from [`InputProcessor<TValue>`](xref:UnityEngine.InputSystem.InputProcessor`1), and implement the [`Process`](xref:UnityEngine.InputSystem.InputProcessor`1) method:

[!code-cs[myvalueprocessor](Packages/com.unity.inputsystem/DocCodeSamples.Tests/ProcessorsExamples.cs#myvalueprocessor)]

> [!IMPORTANT]
> Processors must be __stateless__, because they are not part of the [input state](control-state.md) that the Input System keeps. For this reason, you can't store local states in a processor if the processor changes based on the input value.

## Register the new processor to the Input System

Register the new processor to the Input System. Call [`InputSystem.RegisterProcessor`](xref:UnityEngine.InputSystem.InputSystem) in your initialization code. You can do this locally within the Processor class:

[!code-cs[registernewprocessor](Packages/com.unity.inputsystem/DocCodeSamples.Tests/CustomProcessors.cs#registernewprocessor)]

Your new Processor is now available in the in the [Input Actions Editor](actions-editor.md) and you can also add it in code like this:

[!code-cs[inputactionwithprocessor](Packages/com.unity.inputsystem/DocCodeSamples.Tests/CustomProcessors.cs#inputactionwithprocessor)]

## Customize the Editor UI

To customize the UI for editing your Processor, create a custom [`InputParameterEditor`](xref:UnityEngine.InputSystem.Editor.InputParameterEditor`1) class for it:

[!code-cs[customizeUI](Packages/com.unity.inputsystem/DocCodeSamples.Tests/ProcessorsExamples.cs#customizeUI)]
