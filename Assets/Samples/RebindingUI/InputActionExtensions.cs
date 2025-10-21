using System;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Extension methods to reduce code bloat of this example.
    /// </summary>
    public static class InputActionExtensions
    {
        /// <summary>
        /// Attempts to find an action binding using its binding ID (GUID).
        /// </summary>
        /// <param name="action">The action instance, may be null.</param>
        /// <param name="bindingId">The binding ID (GUID) represented by a string.</param>
        /// <returns>Zero-based index of the binding or -1 if not found.</returns>
        public static int FindBindingById(this InputAction action, string bindingId)
        {
            if (action == null || string.IsNullOrEmpty(bindingId))
                return -1;
            var id = new Guid(bindingId);
            return action.bindings.IndexOf(x => x.id == id);
        }
    }
}
