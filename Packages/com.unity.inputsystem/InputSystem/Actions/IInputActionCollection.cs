using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;

////TODO: move indexer up here

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
        ///
        /// Modifying this property while any of the actions in the collection are enabled will
        /// lead to the actions getting disabled temporarily and then re-enabled.
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
        ///
        /// Modifying this property after bindings in the collection have already been resolved,
        /// will lead to <see cref="InputAction.controls"/> getting refreshed. If any of the actions
        /// in the collection are currently in progress (see <see cref="InputAction.phase"/>),
        /// the actions will remain unaffected and in progress except if the controls currently
        /// driving them (see <see cref="InputAction.activeControl"/>) are no longer part of any
        /// of the selected devices. In that case, the action is <see cref="InputAction.canceled"/>.
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
        /// <seealso cref="InputAction.Enable"/>
        /// <seealso cref="InputAction.enabled"/>
        void Enable();

        /// <summary>
        /// Disable all actions in the collection.
        /// </summary>
        /// <seealso cref="InputAction.Disable"/>
        /// <seealso cref="InputAction.enabled"/>
        void Disable();
    }

    /// <summary>
    /// An extended version of <see cref="IInputActionCollection"/>.
    /// </summary>
    /// <remarks>
    /// This interface will be merged into <see cref="IInputActionCollection"/> in a future (major) version.
    /// </remarks>
    public interface IInputActionCollection2 : IInputActionCollection
    {
        /// <summary>
        /// Iterate over all bindings in the collection of actions.
        /// </summary>
        /// <seealso cref="InputActionMap.bindings"/>
        /// <seealso cref="InputAction.bindings"/>
        /// <seealso cref="InputActionAsset.bindings"/>
        IEnumerable<InputBinding> bindings { get; }

        /// <summary>
        /// Find an <see cref="InputAction"/> in the collection by its <see cref="InputAction.name"/> or
        /// by its <see cref="InputAction.id"/> (in string form).
        /// </summary>
        /// <param name="actionNameOrId">Name of the action as either a "map/action" combination (e.g. "gameplay/fire") or
        /// a simple name. In the former case, the name is split at the '/' slash and the first part is used to find
        /// a map with that name and the second part is used to find an action with that name inside the map. In the
        /// latter case, all maps are searched in order and the first action that has the given name in any of the maps
        /// is returned. Note that name comparisons are case-insensitive.
        ///
        /// Alternatively, the given string can be a GUID as given by <see cref="InputAction.id"/>.</param>
        /// <param name="throwIfNotFound">If <c>true</c>, instead of returning <c>null</c> when the action
        /// cannot be found, throw <c>ArgumentException</c>.</param>
        /// <returns>The action with the corresponding name or <c>null</c> if no matching action could be found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actionNameOrId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="throwIfNotFound"/> is true and the
        /// action could not be found. -Or- If <paramref name="actionNameOrId"/> contains a slash but is missing
        /// either the action or the map name.</exception>
        InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false);

        /// <summary>
        /// Find the index of the first binding that matches the given mask.
        /// </summary>
        /// <param name="mask">A binding. See <see cref="InputBinding.Matches"/> for details.</param>
        /// <param name="action">Receives the action on which the binding was found. If none was found,
        /// will be set to <c>null</c>.</param>
        /// <returns>Index into <see cref="InputAction.bindings"/> of <paramref name="action"/> of the binding
        /// that matches <paramref name="mask"/>. If no binding matches, will return -1.</returns>
        /// <remarks>
        /// For details about matching bindings by a mask, see <see cref="InputBinding.Matches"/>.
        ///
        /// <example>
        /// <code>
        /// var index = playerInput.actions.FindBinding(
        ///     new InputBinding { path = "&lt;Gamepad&gt;/buttonSouth" },
        ///     out var action);
        ///
        /// if (index != -1)
        ///     Debug.Log($"The A button is bound to {action}");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.Matches"/>
        /// <seealso cref="bindings"/>
        int FindBinding(InputBinding mask, out InputAction action);
    }
}
