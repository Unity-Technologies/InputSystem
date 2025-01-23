# Configure Control Type

When you select an Action in the [Actions Editor window](./actions-editor.md), you can edit its properties in the right-hand pane of the window.

The second of these properties is the Control Type.

![Action Properties](Images/ActionProperties.png)

The Control Type setting allows you to select the type of control expected by the action. This limits the types of [composite bindings](composite-bindings.md) and [control types](control-types.md) shown when setting up bindings in the UI, and also limits which controls can be bound interactively to the action. This makes it simpler to select appropriate options when setting up bindings.

For example, if you select **2D axis** as the control type, only those types of controls that can supply a 2D vector as value are available as options for the binding control path, such as a thumb stick or Dpad.

There are more specific control types available which further filter the available bindings, such as "Stick", "Dpad" or "Touch". If you select one of these control types, the list of available controls is further limited to only those controls of those specific types when you [select a binding for your action](add-duplicate-delete-binding.md).

To configure your action's control type, select an option from the **Control Type** drop-down menu.