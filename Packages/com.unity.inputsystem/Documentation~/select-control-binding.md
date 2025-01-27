# Pick a control for a binding

The [control path](control-paths.md) identifies the specific control that a binding is bound to, such as a specific button or stick on a gamepad, or a specific keyboard key.

There are three ways to specify the control path for a binding in the [Actions Editor window](./actions-editor.md). These are:

- **Select the control path from a list**,
- **Select the control path using the listen feature**,
- or **Enter the path directly by typing text**

All three options are described below. For all these options, you must first:

1. Open the [Actions Editor window](./actions-editor.md)
2. Select the **action** you want to edit from the [actions panel](./actions-panel.md)
3. Expand the action to reveal its bindings, or [add a new binding](./add-duplicate-delete-binding.md)
4. Select the **binding** you want to edit

With a binding selected, you can then select the control path using any of these options.

## Select the control path from a list

To select the control path for a binding from a list of available controls:

   1. Select the **Path** dropdown menu.<br/><br/>This displays a hierarchially arranged tree of input devices and controls that the Input System recognizes, which you can browse to find the control you want to bind.

   2. Select the control you want from the list.

![Control Picker](Images/InputControlPicker.png)

Unity filters this list by the Action's [`Control Type`](./control-types.md) property. For example, if the Control type is `Vector2`, you can only select a Control that generates two-dimensional values, like a stick.

The Device and Control tree is organized hierarchically from generic to specific. For example, the __Gamepad__ Control path `<Gamepad>/buttonSouth` matches the lower action button on any gamepad. Alternatively, if you navigate to __Gamepad__ > __More Specific Gamepads__ and select __PS4 Controller__, and then choose the Control path `<DualShockGamepad>/buttonSouth`, this only matches the "Cross" button on PlayStation gamepads, and doesn't match any other gamepads.

## Select the control path using the listen feature

Instead of browsing the tree to find the Control you want, if you have the device connected that you want to bind, it can be easier to let the Input System listen for input from that device. To do this:

1. Select the __Listen__ button.
2. Press the button or actuate the control on the device you want to bind to.
3. While the control picker is in listen mode, all buttons or controls you actuate appear in a list.
4. Select the binding from the list to finalise the binding.


## Enter the path directly by typing text

You can choose to manually type the Binding path as text instead of using the Control picker. To do this:

1. Select the __T__ button next to the Control path popup. This changes the **path** field from a popup menu to a text field, where you can enter any Binding string.
2. Type the control path's binding string into the text field. See [control paths](./control-paths.md) for more details about valid syntax for this field.

Entering control paths as text also allows you to use wildcard (`*`) characters in your Bindings. For example, you can use a Binding path such as `<Touchscreen>/touch*/press` to bind to any finger pressed on the touchscreen, instead of manually binding to `<Touchscreen>/touch0/press`, `<Touchscreen>/touch1/press` and each other numbered touch.

