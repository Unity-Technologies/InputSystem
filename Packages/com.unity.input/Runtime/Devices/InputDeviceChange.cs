using System;
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
        ///
        /// See also <see cref="InputSystem.AddDevice{TDevice}(string)"/> and <see cref="InputDevice.added"/>.
        /// </summary>
        Added,

        /// <summary>
        /// An existing device was removed from the system. This is triggered <em>after</em> the
        /// device has already been removed, i.e. it already has been cleared from <see cref="InputSystem.devices"/>.
        ///
        /// Other than when a device is removed programmatically, this happens when a device
        /// is unplugged from the system. Subsequent to the notification, the system will remove
        /// the <see cref="InputDevice"/> instance from its list and remove the device's
        /// recorded input state.
        ///
        /// See also <see cref="InputSystem.RemoveDevice"/>.
        /// </summary>
        Removed,

        /// <summary>
        /// A device reported by the <see cref="IInputRuntime"/> was <see cref="Removed"/> but was
        /// retained by the system as <see cref="InputSystem.disconnectedDevices">disconnected</see>.
        ///
        /// See also <see cref="InputSystem.disconnectedDevices"/>.
        /// </summary>
        Disconnected,

        /// <summary>
        /// A device that was previously retained as <see cref="Disconnected"/> has been re-discovered
        /// and has been <see cref="Added"/> to the system again.
        ///
        /// See also <see cref="InputSystem.disconnectedDevices"/>.
        /// </summary>
        Reconnected,

        /// <summary>
        /// An existing device was re-enabled after having been <see cref="Disabled"/>.
        ///
        /// See also <see cref="InputSystem.EnableDevice"/> and <see cref="InputDevice.enabled"/>.
        /// </summary>
        Enabled,

        /// <summary>
        /// An existing device was disabled.
        ///
        /// See also <see cref="InputSystem.DisableDevice"/> and <see cref="InputDevice.enabled"/>.
        /// </summary>
        Disabled,

        /// <summary>
        /// The usages on a device have changed.
        ///
        /// This may signal, for example, that what was the right hand XR controller before
        /// is now the left hand controller.
        ///
        /// See also <see cref="InputSystem.SetDeviceUsage(InputDevice,string)"/> and
        /// <see cref="InputControl.usages"/>.
        /// </summary>
        UsageChanged,

        /// <summary>
        /// The configuration of a device has changed.
        ///
        /// This may signal, for example, that the layout used by the keyboard has changed or
        /// that, on a console, a gamepad has changed which player ID(s) it is assigned to.
        ///
        /// See also <see cref="DeviceConfigurationEvent"/> and <see cref="InputSystem.QueueConfigChangeEvent"/>.
        /// </summary>
        ConfigurationChanged,

        /// <summary>
        /// Device is being "soft" reset but in a way that excludes <see cref="Layouts.InputControlLayout.ControlItem.dontReset"/>
        /// controls such as mouse positions. This can happen during application focus changes
        /// (see <see cref="InputSettings.backgroundBehavior"/>) or when <see cref="InputSystem.ResetDevice"/>
        /// is called explicitly.
        ///
        /// This notification is sent before the actual reset happens.
        /// </summary>
        SoftReset,

        /// <summary>
        /// Device is being "hard" reset, i.e. every control is reset to its default value. This happens only
        /// when explicitly forced through <see cref="InputSystem.ResetDevice"/>.
        ///
        /// This notification is sent before the actual reset happens.
        /// </summary>
        HardReset,

        [Obsolete("Destroyed enum has been deprecated.")]
        Destroyed,
    }
}
