using System;

namespace ISX
{
	// By default, actions will start when a source control leaves its default state
	// and will be completed when the control goes back to that state. Modifiers can customize
	// this and also implement logic that signals cancellations (which the default logic never
	// triggers).
	// Modifiers can be stateful and mutate state over time.
	// Modifiers can both stack and nest. If nested, ...
    public abstract class InputActionModifier
    {
	    public abstract InputAction.Phase ProcessValueChange(InputAction action, InputControl control, double time);
    }

    // Convenience class that helps more rapidly implementing modifiers that operate
    // on specific types of controls.
    public abstract class InputActionModifier<TControl> : InputActionModifier
        where TControl : InputControl
    {
	    ////TODO: come up with a way to avoid the double virtual dispatch on these class (a double dispatch that only
	    ////      gains us a typecheck even...)
	    public sealed override InputAction.Phase ProcessValueChange(InputAction action, InputControl control, double time)
	    {
		    var controlOfType = control as TControl;
		    if (controlOfType == null)
			    throw new InvalidOperationException(
				    $"Modifier '{GetType().Name}' expects control of type '{typeof(TControl).Name}' but got control of type '${control.GetType().Name}' instead");
		    
		    return ProcessValueChange(action, controlOfType, time);
	    }

	    public abstract InputAction.Phase ProcessValueChange(InputAction action, TControl control, double time);
    }
}
