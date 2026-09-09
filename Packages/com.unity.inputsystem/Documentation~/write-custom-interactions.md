---
uid: input-system-custom-interactions
---

# Write custom interactions

You can also write a custom Interaction to use in your project. You can use custom Interactions in the UI and code the same way you use built-in Interactions.

Add a class implementing the [`IInputInteraction`](xref:UnityEngine.InputSystem.IInputInteraction) interface, like this:

[!code-cs[custominteraction](Packages/com.unity.inputsystem/DocCodeSamples.Tests/IntroductionInteractions.cs#custominteraction)]

Register your interaction with the Input System in your initialization code:

[!code-cs[registerinteraction](Packages/com.unity.inputsystem/DocCodeSamples.Tests/IntroductionInteractions.cs#registerinteraction)]

Your new Interaction is now available in the [Input Action Asset Editor window](xref:input-system-action-assets).

You can also add it in code using this call:

[!code-cs[useinteraction](Packages/com.unity.inputsystem/DocCodeSamples.Tests/IntroductionInteractions.cs#useinteraction)]
