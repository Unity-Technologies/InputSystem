---
uid: input-system-composite-bindings
---

# Composite Bindings

You might want to have several controls act in unison to mimic a different type of control. The most common example of this is using the W, A, S, and D keys on the keyboard to form a 2D vector control equivalent to mouse deltas or gamepad sticks. Another example is to use two keys to form a 1D axis equivalent to a mouse scroll axis.

This is difficult to implement with normal bindings. You can bind a  [`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl) to an action expecting a `Vector2`, but doing so results in an exception at runtime when the Input System tries to read a `Vector2` from a control that can deliver only a `float`.

Composite bindings (that is, bindings that are made up of other bindings) solve this problem. Composites themselves don't bind directly to controls; instead, they source values from other bindings that do, and then synthesize input on the fly from those values.

To create a composite binding, select the appropriate [composite type](binding-types.md) for your action while [adding a binding in the actions editor](./add-duplicate-delete-binding.md). The types of composite bindings available are as follows:

## Types of composite bindings

The **Add binding (+)** menu contains the following options.

| Value                         | Description                    |
| :---------------------------- | :----------------------------- |
| **Add Binding**               | Adds a [simple binding](./binding-types.md) and is not a composite |
| **Add Positive/Negative Binding** | Adds a 1D axis composite binding made of two button sub-bindings, one that pulls a 1D axis in its negative direction, and another that pulls it in its positive direction. It is implemented in the [`AxisComposite`](xref:UnityEngine.InputSystem.Composites.AxisComposite) class. The output is a `float`.<br/><br/>If Controls from both the `positive` and the `negative` side are actuated, then the resulting value of the axis Composite depends on the **Which side wins** [binding property](./binding-properties-panel-reference.md). |
| **Add Up/Down/Left/Right Composite** | Adds a 2D axis composite binding that represents a 4-way button control like the D-pad on gamepads. Each button sub-binding represents a cardinal direction, is most useful for representing up-down-left-right controls, such as WASD keyboard input. It is implemented in the [`Vector2Composite`](xref:UnityEngine.InputSystem.Composites.Vector2Composite) class. The output is a `Vector2`. This composite's [**mode** property](./binding-properties-panel-reference.md) allows you to choose whether the inputs should be treated as digital or analog controls. |
| **Add Up/Down/Left/Right/Forward/Backward Composite** | Adds a 3D composite binding that represents a 6-way button where two combinations each control one axis of a 3D vector. Implemented in the [`Vector3Composite`](xref:UnityEngine.InputSystem.Composites.Vector3Composite) class. The output is a `Vector3`. <br/><br/> This composite's [**mode** property](./binding-properties-panel-reference.md) allows you to choose whether the inputs should be treated as digital or analog controls. |
| **Add Binding With One Modifier** | Adds a composite with two sub-bindings, named **Binding** and **Modifier**, which requires the user to hold down the **modifier** button in addition to another control from which the actual value of the binding is determined. This can be used, for example, for bindings such as "SHIFT+1". Implemented in the [`OneModifierComposite`](xref:UnityEngine.InputSystem.Composites.OneModifierComposite) class. The buttons can be on any Device, and can be toggle buttons or full-range buttons such as gamepad triggers.<br/><br/>The output is a [value of the same type](control-types-reference.md) as the control bound to the sub-binding named **Binding**. |
| **Add Binding With Two Modifiers** | Adds a composite with three sub-bindings, named **Binding**, **Modifier 1** and **Modifier 2**, which requires the user to hold down two modifier buttons in addition to another control from which the actual value of the binding is determined. This can be used, for example, for bindings such as "SHIFT+CTRL+1". Implemented in the [`TwoModifiersComposite`](xref:UnityEngine.InputSystem.Composites.TwoModifiersComposite) class. The buttons can be on any Device, and can be toggle buttons or full-range buttons such as gamepad triggers.<br/><br/>The output is a [value of the same type](control-types-reference.md) as the control bound to the sub-binding named **Binding**. |

> [!NOTE]
> You can also [create custom composite bindings from code](./create-custom-composite-binding.md)
