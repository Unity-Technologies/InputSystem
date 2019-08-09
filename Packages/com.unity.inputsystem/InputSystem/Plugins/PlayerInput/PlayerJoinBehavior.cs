namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Determines how <see cref="PlayerInputManager"/> joins new players.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <seealso cref="PlayerInputManager"/>
    /// <seealso cref="PlayerInputManager.joinBehavior"/>
    public enum PlayerJoinBehavior
    {
        /// <summary>
        /// Listen for button presses on devices that are not paired to any player. If they occur
        /// and joining is allowed, join a new player using the device the button was pressed on.
        /// </summary>
        JoinPlayersWhenButtonIsPressed,

        JoinPlayersWhenJoinActionIsTriggered,

        /// <summary>
        /// Do not join players automatically. Call <see cref="JoinPlayerFromUI"/> or <see cref="JoinPlayerFromAction"/>
        /// explicitly in order to join new players. Alternatively, just create GameObjects with <see cref="PlayerInput"/>
        /// components directly and they will be joined automatically.
        /// </summary>
        /// <remarks>
        /// This behavior also allows implementing more sophisticated device pairing mechanisms when multiple devices
        /// are involved. While initial engagement required by <see cref="JoinPlayersWhenButtonIsPressed"/> or
        /// <see cref="JoinPlayersWhenJoinActionIsTriggered"/> allows pairing a single device reliably to a player,
        /// additional devices that may be required by a control scheme will still get paired automatically out of the
        /// pool of available devices. This means that, for example, if a given player joins by clicking a mouse button
        /// ...
        /// </remarks>
        JoinPlayersManually,
    }
}
