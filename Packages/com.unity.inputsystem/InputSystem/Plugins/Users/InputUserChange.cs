namespace UnityEngine.Experimental.Input.Plugins.Users
{
    /// <summary>
    /// Indicates what type of change related to an <see cref="InputUser">input user</see> occurred.
    /// </summary>
    /// <seealso cref="InputUser.onChange"/>
    public enum InputUserChange
    {
        /// <summary>
        /// A new user was added to the system.
        /// </summary>
        /// <see cref="InputUser.Add"/>
        Added,

        /// <summary>
        /// An existing user was removed from the user.
        /// </summary>
        /// <see cref="InputUser.Remove"/>
        Removed,

        /// <summary>
        /// An existing user changed the set of devices assigned to the user.
        /// </summary>
        /// <seealso cref="InputUser.GetAssignedInputDevices{TUser}"/>
        /// <seealso cref="InputUser.AssignInputDevice{TUser}"/>
        /// <seealso cref="InputUser.AssignInputDevices{TUser,TDevices}"/>
        DevicesChanged,

        /// <summary>
        /// The user switched to a different set of actions.
        /// </summary>
        /// <remarks>
        /// The changing of actions usually happens when a player changes context. For example,
        /// when going from gameplay to the menu.
        /// </remarks>
        /// <seealso cref="InputUser.GetInputActions{TUser}"/>
        /// <seealso cref="InputUser.SetInputActions{TUser}"/>
        ActionsChanged,

        ControlSchemeChanged,

        NameChanged,

        BindingsChanged,

        /// <summary>
        ///
        /// </summary>
        /// <seealso cref="InputUser.IsInputActive{TUser}"/>
        /// <seealso cref="InputUser.ActivateInput{TUser}"/>
        Activated,

        /// <summary>
        ///
        /// </summary>
        /// <seealso cref="InputUser.IsInputActive{TUser}"/>
        /// <seealso cref="InputUser.PassivateInput{TUser}"/>
        Passivated,
    }
}
