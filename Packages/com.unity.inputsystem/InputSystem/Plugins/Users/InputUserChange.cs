using UnityEngine.InputSystem.LowLevel;

////REVIEW: how do we handle the case where the OS may (through whatever means) recognize individual real-world users,
////        associate a user account with each one, and recognize when a controller is passed from one user to the other?
////        (NUI on Xbox probably gives us that scenario)

namespace UnityEngine.InputSystem.Users
{
    /// <summary>
    /// Indicates what type of change related to an <see cref="InputUser"/> occurred.
    /// </summary>
    /// <seealso cref="InputUser.onChange"/>
    public enum InputUserChange
    {
        /// <summary>
        /// A new user was added to the system.
        /// </summary>
        /// <seealso cref="InputUser.PerformPairingWithDevice"/>
        Added,

        /// <summary>
        /// An existing user was removed from the user.
        /// </summary>
        /// <remarks>
        /// <see cref="InputUser"/>s are only removed when explicitly requested (<see cref="InputUser.UnpairDevicesAndRemoveUser"/>).
        /// </remarks>
        /// <seealso cref="InputUser.UnpairDevicesAndRemoveUser"/>
        Removed,

        /// <summary>
        /// A user has had a device assigned to it.
        /// </summary>
        /// <seealso cref="InputUser.PerformPairingWithDevice"/>
        /// <seealso cref="InputUser.pairedDevices"/>
        DevicePaired,

        /// <summary>
        /// A user has had a device removed from it.
        /// </summary>
        /// <remarks>
        /// This is different from <see cref="DeviceLost"/> in that the removal is intentional. <see cref="DeviceLost"/>
        /// instead indicates that the device was removed due to causes outside of the control of the application (such
        /// as battery loss) whereas DeviceUnpaired indicates the device was removed from the user under the control of
        /// the application itself.
        /// </remarks>
        /// <seealso cref="InputUser.UnpairDevice"/>
        /// <seealso cref="InputUser.UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUser.pairedDevices"/>
        DeviceUnpaired,

        /// <summary>
        /// A device was removed while paired to the user.
        /// </summary>
        /// <remarks>
        /// This scenario happens on battery loss, for example.
        ///
        /// Note that there is still a <see cref="DevicePaired"/> change sent when the device is subsequently removed
        /// from the user.
        /// </remarks>
        /// <seealso cref="InputUser.pairedDevices"/>
        /// <seealso cref="InputUser.lostDevices"/>
        DeviceLost,

        ////REVIEW: should we tie this to the specific device requirement slot in the control scheme?
        /// <summary>
        /// A device that was previously lost (<see cref="DeviceLost"/>) was regained by the user.
        /// </summary>
        /// <remarks>
        /// This can happen, for example, when a gamepad runs out of battery and is then plugged in by wire.
        ///
        /// Note that a device may be permanently lost and instead be replaced by a different device.
        /// </remarks>
        /// <seealso cref="InputUser.lostDevices"/>
        DeviceRegained,

        ////TODO: bring documentation for these back when user management is implemented on Xbox and PS
        AccountChanged,
        AccountNameChanged,
        AccountSelectionInProgress,
        AccountSelectionCanceled,
        AccountSelectionComplete,

        ////REVIEW: send notifications about the matching status of the control scheme? maybe ControlSchemeActivated, ControlSchemeDeactivated,
        ////        and ControlSchemeChanged?

        /// <summary>
        /// A user switched to a different control scheme.
        /// </summary>
        /// <seealso cref="InputUser.ActivateControlScheme(string)"/>
        /// <seealso cref="InputUser.ActivateControlScheme(InputControlScheme)"/>
        ControlSchemeChanged,

        /// <summary>
        /// A user's bound controls have changed, either because the bindings of the user have changed (for example,
        /// due to an override applied with <see cref="InputActionRebindingExtensions.ApplyBindingOverride(InputAction,InputBinding)"/>)
        /// or because the controls themselves may have changed configuration (every time the device of the controls receives
        /// an <see cref="DeviceConfigurationEvent"/>; for example, when the current keyboard layout on a <see cref="Keyboard"/>
        /// changes which in turn modifies the <see cref="InputControl.displayName"/>s of the keys on the keyboard).
        /// </summary>
        ControlsChanged,

        /*
        ////TODO: this is waiting for InputUserSettings
        /// <summary>
        /// A setting in the user's <see cref="InputUserSettings"/> has changed.
        /// </summary>
        /// <remarks>
        /// This is separate from <see cref="BindingsChanged"/> even though bindings are part of user profiles
        /// (<see cref="InputUserSettings.customBindings"/>). The reason is that binding changes often require
        /// </remarks>
        SettingsChanged,
        */
    }
}
