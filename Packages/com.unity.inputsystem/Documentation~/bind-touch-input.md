---
uid: input-system-bind-touch-input
---

# Bind touch input to an action

You can use touch input with [Actions](actions.md), like any other pointer device. To use actions with touch devices:

* Associate [bindings](bindings.md) to the [pointer controls available in the `Pointer` class](xref:UnityEngine.InputSystem.Pointer). For example, `<Pointer>/press` or `<Pointer>/delta`.

This gets input from the primary touch, and any other non-touch pointer devices.

## Get input from multiple touches

If you want to get input from multiple touches in the action:

* Use bindings like `<Touchscreen>/touch3/press` to bind to individual touches.
* Alternatively, use a wildcard binding to bind one action to all touches. For example, `<Touchscreen>/touch*/press`.

If you bind a single action to input from multiple touches, set the action type to [pass-through](about-action-control-types.md#action-type) so the action gets callbacks for each touch, instead of just one.
