# Binding types

The simplest type of binding is where a single control maps directly to the action. For example, a gamepad stick to a "move" action, or a gamepad button to a "jump" action.

Other types of bindings are possible, which are referred to as composite bindings. These types allow you to construct a composite binding from multiple simple bindings.

Some examples of this are:


- You can create a positive/negative composite binding so that two separate controls form an axis, where one control represents the positive direction of the axis, and the other represents the negative. For example, to make the left and right triggers of a gamepad control a single axis used to accelerate and decelerate a vehicle.

- You can create a four-way composite binding to map four keyboard keys to an action whose [control type](control-types.md) is a 2D vector, so that each of the keys maps to up, down, left, and right respectively.

- You can create a modifier composite binding ...

