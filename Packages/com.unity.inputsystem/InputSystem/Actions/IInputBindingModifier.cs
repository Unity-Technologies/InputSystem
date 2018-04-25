using UnityEngine.Experimental.Input.Utilities;

////TODO: modifiers should be able to not just control phase flow but also what value is reported through the action

////REVIEW: what about putting an instance of one of these on every resolved control instead of sharing it between all controls resolved from a binding?

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Interface for modifying the behavior of actions.
    /// </summary>
    /// <remarks>
    /// By default, actions will start when a source control leaves its default state
    /// and will be completed when the control goes back to that state. Modifiers can customize
    /// this and also implement logic that signals cancellations (which the default logic never
    /// triggers).
    ///
    /// Modifiers can be stateful and mutate state over time.
    /// </remarks>
    public interface IInputBindingModifier
    {
        void Process(ref InputAction.ModifierContext context);
        void Reset();
    }

    /// <summary>
    /// Extended action modifier interface that also allows processing values returned
    /// from actions.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <seealso cref="InputAction.CallbackContext.GetValue{TValue}"/>
    public interface IInputBindingModifier<TValue> : IInputBindingModifier
    {
        bool ProcessValue(ref InputAction.ModifierContext context, ref TValue value);
    }

    internal static class InputBindingModifier
    {
        public static TypeTable s_Modifiers;
    }
}
