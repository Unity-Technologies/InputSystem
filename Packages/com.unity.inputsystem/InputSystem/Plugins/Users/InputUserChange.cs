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
        Added,

        /// <summary>
        /// An existing user was removed from the user.
        /// </summary>
        Removed,

        /// <summary>
        /// An existing user changed the set of devices assigned to the user.
        /// </summary>
        /// <seealso cref="InputUser.devices"/>
        DevicesChanged,

        /// <summary>
        /// The user switched to a different set of actions.
        /// </summary>
        /// <remarks>
        /// The changing of actions usually happens when a player changes context. For example,
        /// when going from gameplay to the menu.
        /// </remarks>
        /// <seealso cref="InputUser.activeActions"/>
        ActionsChanged,

        ControlSchemeChanged,

        NameChanged,

        BindingsChanged,
    }
}
