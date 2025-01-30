# Bind touch input to an action

You can use touch input with \[Actions\](), like any other pointer device. To use actions with touch devices:

* Associate \[bindings\]() to the \[pointer controls available in the Pointer class\](). For example, `<Pointer>/press` or `<Pointer>/delta`. 

This gets input from the primary touch, and any other non-touch pointer devices.

If you want to get input from multiple touches in the action:

* Use bindings like `<Touchscreen>/touch3/press` to bind to individual touches.  
* Alternatively, use a wildcard binding to bind one Action to all touches. For example, `<Touchscreen>/touch*/press`.

If you bind a single action to input from multiple touches, set the action type to [pass-through](http://localhost:57437/com.unity.inputsystem@1.12/manual/RespondingToActions.html#pass-through) so the actio2n gets callbacks for each touch, instead of just one.