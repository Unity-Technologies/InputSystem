---
uid: input-system-editor-features
---
# Input System Editor Features

This section describes how the Input System integrates with the Unity Editor, which allows you to read input in edit mode, debug input values, and set up automated input tests.

### [Using Input in the Editor](xref:input-system-use-in-editor)

Unlike Unity's old Input Manager, the Input System package allows you to read input  from within [Editor window code](https://docs.unity3d.com/Manual/editor-EditorWindows.html) as well. ([Read more](xref:input-system-use-in-editor))

### [The Input Debugger](xref:input-system-debugging)

When something isn't working as expected, the quickest way to troubleshoot what's wrong is the Input Debugger in the Unity Editor. The Input Debugger provides access to the activity of the Input System in both the Editor and the connected Players. ([Read more](xref:input-system-debugging))

### [Automated Input Testing](xref:input-system-testing)

The Input System has built-in support for writing automated input tests. You can drive input entirely from code, without any dependencies on platform backends and physical hardware devices. The automated input tests you write consider the generated input to be the same as input generated at runtime by actual platform code. ([Read more](xref:input-system-testing))
