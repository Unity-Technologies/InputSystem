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

        ControlSchemeChanged,

        NameChanged,

        HandleChanged,

        BindingsChanged,
    }
}
