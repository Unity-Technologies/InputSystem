using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Similar to IObservable but operates on whole sequences to reduce cost of indirect calls.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISequenceObservable<T>
    {
        /// <summary>
        /// Called when all of the sequence have been processed.
        /// </summary>
        public void OnSequenceCompleted();

        /// <summary>
        /// Process an unexpected error.
        /// </summary>
        /// <param name="e">The associated exception</param>
        public void OnSequenceError(Exception e);

        /// <summary>
        /// Processes the next consecutive sequence.
        /// </summary>
        /// <param name="values">The sequence of ordered values.</param>
        public void OnNextSequence(ReadOnlySpan<T> values);
    }
}
