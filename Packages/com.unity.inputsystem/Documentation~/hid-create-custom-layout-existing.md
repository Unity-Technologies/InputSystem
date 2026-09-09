---
uid: input-system-use-existing-layout
---

# Use an existing input device to create a layout

To use one of the existing C# [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) classes in code to interface with a device, you can build on an existing layout using JSON:

[!code-json[myDeviceJson](Packages/com.unity.inputsystem/DocCodeSamples.Tests/HidCreateCustomLayoutExisting.cs#myDeviceJson)]

You then register your layout with the system and then instantiate it:

[!code-cs[registerAndCreate](Packages/com.unity.inputsystem/DocCodeSamples.Tests/HidCreateCustomLayoutExisting.cs#registerAndCreate)]
