
## Control usages

**Control usage** refers to the meaning of a control. In contrast to the name of a control which describes its physical end point on a device (such as `Button south`), the usage identifies the particular role of a control. For example, the usage `Back` identifies a control generally used to move backwards in the navigation history of a UI, and the usage `Submit` identifies a Control generally used to confirm a selection in the UI.

On a keyboard, the escape key that generally fulfills this role of `Back`, whereas on a gamepad, it is generally the `B` or `Circle` button.

Some devices might not have a control that generally fulfills this function and so might not have any control with the `Back` usage.

By looking up controls by usage rather than by name, you can locate the correct control to use for certain standardized situation without needing to know the particulars of the device or platform.

To bind a control to an action by usage:

1. Follow the documentation to [add a binding to an action](./add-duplicate-delete-binding.md)
2. When selecting a binding from the **Control Path** dropdown menu, select **Usages**
3. Select a usage from the list of usages displayed.

You can access a Control's usages using the [`InputControl.usages`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_usages) property.

Usages can be arbitrary strings. However, there is a particular set of common usages, which predefined in the API in the form of the [`CommonUsages`](../api/UnityEngine.InputSystem.CommonUsages.html) static class. See [`CommonUsages`](../api/UnityEngine.InputSystem.CommonUsages.html) for an overview.
