using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A collection of input actions (see <see cref="InputAction"/>).
    /// </summary>
    /// <seealso cref="InputActionMap"/>
    /// <seealso cref="InputActionAsset"/>
    public interface IInputActionCollection : IEnumerable<InputAction>
    {
        /// <summary>
        /// Optional mask applied to all bindings in the collection.
        /// </summary>
        /// <remarks>
        /// If this is not null, only bindings that match the mask will be used.
        /// </remarks>
        InputBinding? bindingMask { get; set; }

        ////REVIEW: should this allow restricting to a set of controls instead of confining it to just devices?
        /// <summary>
        /// Devices to use with the actions in this collection.
        /// </summary>
        /// <remarks>
        /// If this is set, actions in the collection will exclusively bind to devices
        /// in the given list. For example, if two gamepads are present in the system yet
        /// only one gamepad is listed here, then a "&lt;Gamepad&gt;/leftStick" binding will
        /// only bind to the gamepad in the list and not to the one that is only available
        /// globally.
        /// </remarks>
        ReadOnlyArray<InputDevice>? devices { get; set; }

        /// <summary>
        /// List of control schemes defined for the set of actions.
        /// </summary>
        /// <remarks>
        /// Control schemes are optional and the list may be empty.
        /// </remarks>
        ReadOnlyArray<InputControlScheme> controlSchemes { get; }

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

        /// <summary>
        /// Enable all actions in the collection.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disable all actions in the collection.
        /// </summary>
        void Disable();
    }
}
