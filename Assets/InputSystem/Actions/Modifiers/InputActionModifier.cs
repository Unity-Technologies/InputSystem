namespace ISX
{
    public class InputActionModifier
    {
    }
	
	// Convenience class that helps more rapidly implementing modifiers that operate
	// on specific types of controls.
	public abstract class InputActionModifier<TControl> : InputActionModifier
		where TControl : InputControl
	{
	}
}
