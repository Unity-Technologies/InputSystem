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

        ////TODO
        ActionTriggered,

        ////REVIEW: should this just be a general notification we fire whenever we bind controls on an action?
        ////        (and fire it regardless of whether the action is enabled or not)
        /// <summary>
        /// An action had its set of bound controls change while the action
        /// was enabled.
        /// </summary>
        /// <seealso cref="InputAction.controls"/>
        BoundControlsHaveChangedWhileEnabled,
    }
}
