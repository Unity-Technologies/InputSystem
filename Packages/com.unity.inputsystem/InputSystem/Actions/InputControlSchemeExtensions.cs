using System;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Extensions to various action-related APIs to ease working with control schemes.
    /// </summary>
    public static class InputControlSchemeExtensions
    {
        /// <summary>
        /// Enable all actions in the given map but only with bindings
        /// </summary>
        /// <param name="map"></param>
        /// <param name="scheme"></param>
        /// <remarks>
        /// </remarks>
        public static void Enable(this InputActionMap map, InputControlScheme scheme)
        {
            throw new NotImplementedException();
        }

        public static void Enable(this InputAction action, InputControlScheme scheme)
        {
            throw new NotImplementedException();
        }

        public static void Enable(this InputActionAsset asset, InputControlScheme scheme)
        {
            throw new NotImplementedException();
        }

        public static void Enable(this InputActionAssetReference assetReference, InputControlScheme scheme)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Given an existing <paramref name="device">input device</paramref>, try to find a control scheme in
        /// <paramref name="asset"/> that matches the device with one of its required or optional devices.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static InputControlScheme? FindFirstControlSchemeMatchingDevice(this InputActionAsset asset,
            InputDevice device)
        {
            throw new NotImplementedException();
        }
    }
}
