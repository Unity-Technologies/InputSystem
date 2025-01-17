# Binding types

Bindings have a **type** which can be **simple** or **composite**.

A simple binding is where a single control maps directly to an action. For example, a gamepad stick to a "move" action, or a gamepad button to a "jump" action.

Composite bindings allow you to construct a binding from multiple simple bindings.

When you [add a binding](add-duplicate-delete-binding.md) you must select the appropriate binding type for your action.

Some examples of composite bindings are:

- A **four-way** composite binding, where four keyboard keys map to an action whose [control type](control-types.md) is a 2D vector, so that each of the keys maps to up, down, left, and right respectively. In this scenario, the four key bindings are simple bindings grouped together into into the composite four-way binding.

- You can create a **modifier** composite binding, where one control represents the main binding, and a second control represents a "modifier key" to alter the effect of the main binding - such as colding down the control key on a keyboard before also pressing a letter key. In this scenario, the two separate key bindings are simple bindings grouped together into the composite modifier binding.

For a full list of composite binding types, see [Composite bindings](composite-bindings.md).
