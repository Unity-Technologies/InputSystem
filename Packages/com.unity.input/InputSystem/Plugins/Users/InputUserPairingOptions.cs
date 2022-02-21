using System;

namespace UnityEngine.InputSystem.Users
{
    /// <summary>
    /// Options to modify the behavior on <see cref="InputUser.PerformPairingWithDevice"/>.
    /// </summary>
    [Flags]
    public enum InputUserPairingOptions
    {
        /// <summary>
        /// Default behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// Even if the device is already paired to a user account at the platform level, force the user to select
        /// an account.
        /// </summary>
        /// <remarks>
        /// This is only supported on Xbox and Switch, at the moment.
        ///
        /// On PS4, this is ignored as account pairing is under system control. If the user wants to switch accounts,
        /// he/she does so by pressing the PS4 button on the controller.
        ///
        /// On Xbox, this option will bring up the account picker even if the device is already paired to a user.
        /// This behavior is useful to allow the player to change accounts.
        ///
        /// On platforms other than Xbox and Switch, this option is ignored.
        /// </remarks>
        ForcePlatformUserAccountSelection = 1 << 0,

        /// <summary>
        /// Suppress user account selection when supported at the platform level and a device is not currently paired
        /// to a user account.
        /// </summary>
        /// <remarks>
        /// On Xbox, if a device that does not currently have a user account logged in on it is paired to an
        /// <see cref="InputUser"/>, no account picker will come up and the device will be used without an associated
        /// user account.
        ///
        /// On Switch, this prevents the user management applet from coming up.
        /// </remarks>
        ForceNoPlatformUserAccountSelection = 1 << 1,

        /// <summary>
        /// If the user already has paired devices, unpair them first.
        /// </summary>
        UnpairCurrentDevicesFromUser = 1 << 3,
    }
}
