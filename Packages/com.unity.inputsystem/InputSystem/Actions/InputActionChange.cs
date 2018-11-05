namespace UnityEngine.Experimental.Input
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
        /// An <see cref="InputAction"/> was <see cref="InputActionPhase.Started">started</see>,
        /// <see cref="InputActionPhase.Performed">performed</see> or <see cref="InputActionPhase.Cancelled">
        /// cancelled</see>.
        /// </summary>
        /// <seealso cref="InputAction.started"/>
        /// <seealso cref="InputAction.performed"/>
        /// <seealso cref="InputAction.cancelled"/>
        ActionTriggered,

        ////TODO: turn this into BoundControlsChanged and fire it whenever we change the set of controls on an action or map
        /// <summary>
        /// An action had its set of bound controls change while the action
        /// was enabled.
        /// </summary>
        /// <seealso cref="InputAction.controls"/>
        BoundControlsHaveChangedWhileEnabled,
    }
}
