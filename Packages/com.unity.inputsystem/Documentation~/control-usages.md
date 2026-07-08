---
uid: input-system-control-usages
---

# Control usages

**Control usage** refers to the meaning of a control. In contrast to the name of a control which describes its physical end point on a device (such as `Button south`), the usage identifies the particular role of a control. For example, the usage `Back` identifies a control generally used to move backwards in the navigation history of a UI, and the usage `Submit` identifies a control generally used to confirm a selection in the UI.

On a keyboard, the escape key that generally fulfills this role of `Back`, whereas on a gamepad, it is generally the `B` or `Circle` button.

Some devices might not have a control that generally fulfills this function and so might not have any control with the `Back` usage.

By looking up controls by usage rather than by name when [selecting a control for a binding](select-control-binding.md), you can locate the correct control to use for certain standardized situation without needing to know the specific details of the device or platform.

You can access a control's usages using the [`InputControl.usages`](xref:UnityEngine.InputSystem.InputControl) property.

Usages can be arbitrary strings. However, there is a particular set of common usages, which predefined in the API in the form of the [`CommonUsages`](xref:UnityEngine.InputSystem.CommonUsages) static class. Refer to [`CommonUsages`](xref:UnityEngine.InputSystem.CommonUsages) for an overview.

## Additional resources

- [Select a control for binding](select-control-binding.md)
