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
        SuppressStateUpdates,

        /// <summary>
        /// Input events will be processed for state updates and input action interaction updates but interaction
        /// event notifications will be suppressed.
        /// </summary>
        SuppressActionEventNotifications
    }
}
