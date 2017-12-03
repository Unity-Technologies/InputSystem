using ISX.LowLevel;

namespace ISX
{
    /// <summary>
    /// Indicates what type of change related to an input device occurred.
    /// </summary>
    /// <seealso cref="InputSystem.onDeviceChange"/>
    public enum InputDeviceChange
    {
        /// <summary>
        /// A new device was added to the system.
        /// </summary>
        /// <seealso cref="InputSystem.AddDevice"/>
        Added,

        /// <summary>
        /// An existing device was removed from the system.
        /// </summary>
        /// <seealso cref="InputSystem.RemoveDevice"/>
        Removed,

        /// <summary>
        /// A previously added device was re-connected after having been disconnected.
        /// </summary>
        /// <seealso cref="LowLevel.ConnectEvent"/>
        /// <seealso cref="InputSystem.QueueConnectEvent"/>
        Connected,

        /// <summary>
        /// A previously added device was disconnected but remains added to the system.
        /// </summary>
        /// <seealso cref="LowLevel.DisconnectEvent"/>
        /// <seealso cref="InputSystem.QueueDisconnectEvent"/>
        Disconnected,

        /// <summary>
        /// The usages on a device have changed.
        /// </summary>
        /// <remarks>
        /// This may signal, for example, that what was the right hand XR controller before
        /// is now the left hand controller.
        /// </remarks>
        /// <seealso cref="InputSystem.SetUsage"/>
        /// <seealso cref="InputControl.usages"/>
        UsageChanged,

        VariantChanged,

        /// <summary>
        /// The configuration of a device has changed.
        /// </summary>
        /// <remarks>
        /// This may signal, for example, that the layout used by the keyboard has changed or
        /// that, on a console, a gamepad has changed which player ID(s) it is assigned to.
        /// </remarks>
        /// <seealso cref="LowLevel.ConfigChangeEvent"/>
        /// <seealso cref="InputSystem.QueueConfigChangeEvent"/>
        ConfigurationChanged
    }
}
