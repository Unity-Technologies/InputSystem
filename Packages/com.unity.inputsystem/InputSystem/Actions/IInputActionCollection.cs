using System.Collections.Generic;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A collection of <see cref="InputAction">input actions</see>.
    /// </summary>
    public interface IInputActionCollection : IEnumerable<InputAction>
    {
        /// <summary>
        /// Optional mask applied to all bindings in the collection.
        /// </summary>
        /// <remarks>
        /// </remarks>
        InputBinding? bindingMask { get; }

        void SetBindingMask(InputBinding bindingMask);
        void ClearBindingMask();

        /// <summary>
        /// Check whether the given action is contained in this collection.
        /// </summary>
        /// <param name="action">An arbitrary input action.</param>
        /// <returns>True if the given action is contained in the collection, false if not.</returns>
        /// <remarks>
        /// Calling this method will not allocate GC memory (unlike when iterating generically
        /// over the collection). Also, a collection may have a faster containment check rather than
        /// having to search through all its actions.
        /// </remarks>
        bool Contains(InputAction action);
    }
}
