---
uid: input-system-create-actions-in-code
---

# Create actions in code

You can manually create and configure actions entirely in code, including assigning the bindings. This also works at runtime in the Player. For example:

[!code-cs[configurefromcode](Packages/com.unity.inputsystem/DocCodeSamples.Tests/ConfigureInputfromCode.cs#configurefromcode)]

Any action that you create in this way during Play mode doesn't persist in the input action asset after you exit Play mode. This means you can test your application in a realistic manner in the Editor without having to worry about inadvertently modifying the asset.
