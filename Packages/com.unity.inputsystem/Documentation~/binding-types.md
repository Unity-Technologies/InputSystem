---
uid: input-system-binding-types
---

# Binding types

You can configure how bindings map to actions with binding types. The following binding types are available:

- **Simple**: A single control maps directly to an action. For example, a gamepad stick to a `Move` action, or a gamepad button to a `Jump` action.
- **Composite**: Construct a binding from multiple simple bindings.

When you [add a binding](add-duplicate-delete-binding.md) you must select the appropriate binding type for your action.

Some examples of composite bindings are:

- A **four-way** composite binding, where four keyboard keys map to an action whose [control type](control-types-reference.md) is a 2D vector, so that each of the keys maps to up, down, left, and right respectively. In this scenario, the four key bindings are simple bindings grouped together into into the composite four-way binding.

- A **modifier** composite binding, where one control represents the main binding, and a second control represents a modifier key which changes the effect of the main binding - such as holding down the control key on a keyboard before also pressing a letter key. In this scenario, the two separate key bindings are simple bindings grouped together into the composite modifier binding.

For a full list of composite binding types, refer to [Composite bindings](composite-bindings.md).
