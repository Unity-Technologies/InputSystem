namespace UnityEngine.InputSystem.Experimental
{
    public static class PriorityExtensions
    {
        /// <summary>
        /// Applies a priority to the current node used to prioritize events in case multiple events would be fired
        /// based on a single underlying signal at the same discrete time step.
        /// </summary>
        /// <param name="source">The node to which to apply the priority.</param>
        /// <param name="priority">The priority to be use. A higher priority takes precedence over a a lower priority.
        /// If this is negative no priority will be used.</param>
        /// <typeparam name="TSource">The node for which to apply priority.</typeparam>
        /// <returns><paramref name="source"/> to which the priority was set.</returns>
        /// <remarks>If priority is set multiple times, subsequent calls will override previous priority.
        /// Default priority is zero.</remarks>
        public static TSource Priority<TSource>(this TSource source, int priority)
            where TSource : IObservableInput
        {
            return source;
        }
    }
}