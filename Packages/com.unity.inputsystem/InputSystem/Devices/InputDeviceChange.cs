using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Indicates what type of change related to an <see cref="InputDevice">input device</see> occurred.
    /// </summary>
    /// <remarks>
    /// Use <see cref="InputSystem.onDeviceChange"/> to receive notifications about changes
    /// to the input device setup in the system.
    ///
    /// <example>
    /// <code>
    /// InputSystem.onDeviceChange +=
    ///     (device, change) =>
    ///     {
    ///         switch (change)
    ///         {
    ///             case InputDeviceChange.Added:
    ///                 Debug.Log($"Device {device} was added");
    ///                 break;
    ///             case InputDeviceChange.Removed:
    ///                 Debug.Log($"Device {device} was removed");
    ///                 break;
    ///         }
    ///     };
    /// </code>
    /// </example>
    /// </remarks>
    public enum InputDeviceChange
    {
        /// <summary>
        /// A new device was added to the system. This is triggered <em>after</em> the device
        /// has already been added, i.e. it already appears on <see cref="InputSystem.devices"/>.
        /// </summary>
        /// <seealso cref="InputSystem.AddDevice(string,string,string)"/>
        /// <seealso cref="InputSystem.AddDevice{TDevice}(string)"/>
        /// <seealso cref="InputDevice.added"/>
        Added,

        /// <summary>
        /// An existing device was removed from the system. This is triggered <em>after</em> the
        /// device has already been removed, i.e. it already has been cleared from <see cref="InputSystem.devices"/>.
        /// </summary>
        /// <remarks>
        /// Other than when a device is removed programmatically, this happens when a device
        /// is unplugged from the system. Subsequent to the notification, the system will remove
        /// the <see cref="InputDevice"/> instance from its list and remove the device's
        /// recorded input state.
        /// </remarks>
        /// <seealso cref="InputSystem.RemoveDevice"/>
        Removed,

        /// <summary>
        /// A device reported by the <see cref="IInputRuntime"/> was <see cref="Removed"/> but was
        /// retained by the system as <see cref="InputSystem.disconnectedDevices">disconnected</see>.
        /// </summary>
        /// <seealso cref="InputSystem.disconnectedDevices"/>
        Disconnected,

        /// <summary>
        /// A device that was previously retained as <see cref="Disconnected"/> has been re-discovered
        /// and has been <see cref="Added"/> to the system again.
        /// </summary>
        /// <seealso cref="InputSystem.disconnectedDevices"/>
        /// <seealso cref="IInputRuntime.onDeviceDiscovered"/>
        Reconnected,

        /// <summary>
        /// An existing device was re-enabled after having been <see cref="Disabled"/>.
        /// </summary>
        /// <seealso cref="InputSystem.EnableDevice"/>
        /// <seealso cref="InputDevice.enabled"/>
        Enabled,

        /// <summary>
        /// An existing device was disabled.
        /// </summary>
        /// <seealso cref="InputSystem.DisableDevice"/>
        /// <seealso cref="InputDevice.enabled"/>
        Disabled,

        /// <summary>
        /// The usages on a device have changed.
        /// </summary>
        /// <remarks>
        /// This may signal, for example, that what was the right hand XR controller before
        /// is now the left hand controller.
        /// </remarks>
        /// <seealso cref="InputSystem.SetDeviceUsage(InputDevice,InternedString)"/>
        /// <seealso cref="InputControl.usages"/>
        UsageChanged,

        /// <summary>
        /// The configuration of a device has changed.
        /// </summary>
        /// <remarks>
        /// This may signal, for example, that the layout used by the keyboard has changed or
        /// that, on a console, a gamepad has changed which player ID(s) it is assigned to.
        /// </remarks>
        /// <seealso cref="DeviceConfigurationEvent"/>
        /// <seealso cref="InputSystem.QueueConfigChangeEvent"/>
        ConfigurationChanged,

        ////TODO: fire this when we purge disconnected devices
        Destroyed,
    }
}
