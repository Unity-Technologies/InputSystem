namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Indicates what type of change related to an <see cref="InputAction">input action</see> occurred.
    /// </summary>
    /// <seealso cref="InputSystem.onActionChange"/>
    public enum InputActionChange
    {
        /// <summary>
        /// An individual action was enabled.
        /// </summary>
        /// <seealso cref="InputAction.Enable"/>
        ActionEnabled,

        /// <summary>
        /// An individual action was disabled.
        /// </summary>
        /// <seealso cref="InputAction.Disable"/>
        ActionDisabled,

        /// <summary>
        /// An <see cref="InputActionMap">action map</see> was enabled.
        /// </summary>
        /// <seealso cref="InputActionMap.Enable"/>
        ActionMapEnabled,

        /// <summary>
        /// An <see cref="InputActionMap">action map</see> was disabled.
        /// </summary>
        /// <seealso cref="InputActionMap.Disable"/>
        ActionMapDisabled,

        /// <summary>
        /// An <see cref="InputAction"/> was started.
        /// </summary>
        /// <seealso cref="InputAction.started"/>
        /// <seealso cref="InputActionPhase.Started"/>
        ActionStarted,

        /// <summary>
        /// An <see cref="InputAction"/> was performed.
        /// </summary>
        /// <seealso cref="InputAction.performed"/>
        /// <seealso cref="InputActionPhase.Performed"/>
        ActionPerformed,

        /// <summary>
        /// An <see cref="InputAction"/> was cancelled.
        /// </summary>
        /// <seealso cref="InputAction.cancelled"/>
        /// <seealso cref="InputActionPhase.Cancelled"/>
        ActionCancelled,

        /// <summary>
        ///
        /// </summary>
        BoundControlsAboutToChange,

        /// <summary>
        ///
        /// </summary>
        /// <seealso cref="InputAction.controls"/>
        BoundControlsChanged,
    }
}
