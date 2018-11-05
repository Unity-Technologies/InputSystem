using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Indicates what type of change related to an <see cref="InputDevice">input device</see> occurred.
    /// </summary>
    /// <seealso cref="InputSystem.onDeviceChange"/>
    public enum InputDeviceChange
    {
        /// <summary>
        /// A new device was added to the system.
        /// </summary>
        /// <seealso cref="InputSystem.AddDevice(string,string,string)"/>
        /// <seealso cref="InputSystem.AddDevice{TDevice}(string)"/>
        /// <seealso cref="InputDevice.added"/>
        Added,

        /// <summary>
        /// An existing device was removed from the system.
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

        LayoutVariantChanged,

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

        ////REVIEW: it doesn't seem smart to deliver this high-frequency change on the same path
        ////        as the other low-frequency changes
        StateChanged,

        ////TODO: nuke this along with the entire Current machinery
        CurrentChanged
    }
}
