using UnityEngine.Experimental.Input.Utilities;

////TODO: rename to IInputBindingInteraction (and RegisterBindingInteraction)

////REVIEW: what about putting an instance of one of these on every resolved control instead of sharing it between all controls resolved from a binding?

////REVIEW: can we have multiple interactions work together on the same binding? E.g. a 'Press' causing a start and a 'Release' interaction causing a performed

////REVIEW: have a default interaction so that there *always* is an interaction object when processing triggers?

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Interface for interaction patterns that drive actions.
    /// </summary>
    /// <remarks>
    /// By default, actions will start when a source control leaves its default state
    /// and will be completed when the control goes back to that state. Interactions can customize
    /// this and also implement logic that signals cancellations (which the default logic never
    /// triggers).
    ///
    /// Interactions can be stateful and mutate state over time.
    /// </remarks>
    public interface IInputInteraction
    {
        void Process(ref InputInteractionContext context);
        void Reset();
    }

    internal static class InputInteraction
    {
        public static TypeTable s_Interactions;
    }
}
