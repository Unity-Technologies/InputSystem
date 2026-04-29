using System;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Policy defining how the Input System will react to <see cref="InputEvent"/> instances marked as
    /// <see cref="InputEvent.handled"/> (Or marked handled via <see cref="InputEventPtr.handled"/>).
    /// </summary>
    internal enum InputEventHandledPolicy
    {
        /// <summary>
        /// Input events will be discarded directly and not propagate for state changes.
        /// </summary>
        [Obsolete("Use SuppressActionEventNotifications instead. SuppressStateUpdates desynchronizes Input System state from source state, leading to undefined behavior.", error: false)]
        SuppressStateUpdates,

        /// <summary>
        /// Input events will be processed for state updates and input action interaction updates but interaction
        /// event notifications will be suppressed.
        /// </summary>
        SuppressActionEventNotifications,

        /// <summary>
        /// The default input event handling policy.
        /// </summary>
        Default = SuppressActionEventNotifications
    }
}
