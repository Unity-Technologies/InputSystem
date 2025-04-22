namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Policy defining how the Input System will process <see cref="InputEvent"/> instances marked as
    /// <see cref="InputEvent.handled"/>.
    /// </summary>
    public enum InputEventHandledPolicy
    {
        /// <summary>
        /// Input events will be discarded and not propagated for neither state updates nor notifications.
        /// </summary>
        SuppressProcessing,

        /// <summary>
        /// Input events will be processed for state updates but will not trigger interaction nor phase notifications.
        /// </summary>
        SuppressNotifications
    }
}
