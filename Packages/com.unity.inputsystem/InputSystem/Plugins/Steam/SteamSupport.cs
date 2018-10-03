#if (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using System;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.Steam
{
    /// <summary>
    /// Adds support for Steam controllers.
    /// </summary>
    public static class SteamSupport
    {
        /// <summary>
        /// Wrapper around the Steam controller API.
        /// </summary>
        /// <remarks>
        /// This must be set by user code for Steam controller support to become functional.
        /// </remarks>
        public static ISteamControllerAPI api
        {
            get { return s_API; }
            set
            {
                s_API = value;
                if (value != null && !s_OnUpdateHookedIn)
                {
                    InputSystem.onBeforeUpdate += type => UpdateControllers();
                    s_OnUpdateHookedIn = true;
                }
            }
        }

        /// <summary>
        /// If enabled, if Steam support is in use (i.e. if <see cref="api"/> has been set), then
        /// any <see cref="Gamepad"/> device that isn't using the <see cref="SteamController.kSteamInterface"/>
        /// interface will automatically be disabled.
        /// </summary>
        /// <remarks>
        /// Makes sure that input isn't picked up in parallel through both Unity's own gamepad support and
        /// through Steam's controller support.
        ///
        /// Enabled by default.
        /// </remarks>
        internal static bool disableNonSteamGamepads
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        internal static ISteamControllerAPI GetAPIAndRequireItToBeSet()
        {
            if (s_API == null)
                throw new InvalidOperationException("ISteamControllerAPI implementation has not been set on SteamSupport");
            return s_API;
        }

        internal static SteamHandle<SteamController>[] s_ConnectedControllers;
        internal static SteamController[] s_InputDevices;
        internal static int s_InputDeviceCount;
        internal static bool s_OnUpdateHookedIn;
        internal static bool s_OnActionChangeHookedIn;
        internal static ISteamControllerAPI s_API;

        private const int STEAM_CONTROLLER_MAX_COUNT = 16;

        /// <summary>
        /// Enable support for the Steam controller API.
        /// </summary>
        public static void Initialize()
        {
            // We use this as a base layout.
            InputSystem.RegisterLayout<SteamController>();

            if (api != null && !s_OnUpdateHookedIn)
            {
                InputSystem.onBeforeUpdate +=
                    type => UpdateControllers();
                s_OnUpdateHookedIn = true;
            }
        }

        private static void UpdateControllers()
        {
            if (api == null)
                return;

            // Update controller state.
            api.RunFrame();

            // Check if we have any new controllers have appeared.
            if (s_ConnectedControllers == null)
                s_ConnectedControllers = new SteamHandle<SteamController>[STEAM_CONTROLLER_MAX_COUNT];
            var numConnectedControllers = api.GetConnectedControllers(s_ConnectedControllers);
            for (var i = 0; i < numConnectedControllers; ++i)
            {
                var handle = s_ConnectedControllers[i];

                // See if we already have a device for this one.
                if (s_InputDevices != null)
                {
                    SteamController existingDevice = null;
                    for (var n = 0; n < s_InputDeviceCount; ++n)
                    {
                        if (s_InputDevices[n].handle == handle)
                        {
                            existingDevice = s_InputDevices[n];
                            break;
                        }
                    }

                    // Yes, we do.
                    if (existingDevice != null)
                        continue;
                }

                // No, so create a new device.
                var controllerLayouts = InputSystem.ListLayoutsBasedOn("SteamController");
                foreach (var layout in controllerLayouts)
                {
                    // Rather than directly creating a device with the layout, let it go through
                    // the usual matching process.
                    var device = InputSystem.AddDevice(new InputDeviceDescription
                    {
                        interfaceName = SteamController.kSteamInterface,
                        product = layout
                    });

                    // Make sure it's a SteamController we got.
                    var steamDevice = device as SteamController;
                    if (steamDevice == null)
                    {
                        Debug.LogError(string.Format(
                            "InputDevice created from layout '{0}' based on the 'SteamController' layout is not a SteamController",
                            device.layout));
                        continue;
                    }

                    // Resolve the controller's actions.
                    steamDevice.InvokeResolveActions();

                    // Assign it the Steam controller handle.
                    steamDevice.handle = handle;

                    ArrayHelpers.AppendWithCapacity(ref s_InputDevices, ref s_InputDeviceCount, steamDevice);
                }
            }

            // Update all controllers we have.
            for (var i = 0; i < s_InputDeviceCount; ++i)
            {
                var device = s_InputDevices[i];
                var handle = device.handle;

                // Check if the device still exists.
                var stillExists = false;
                for (var n = 0; n < numConnectedControllers; ++n)
                    if (s_ConnectedControllers[n] == handle)
                    {
                        stillExists = true;
                        break;
                    }

                // If not, remove it.
                if (!stillExists)
                {
                    ArrayHelpers.EraseAtByMovingTail(s_InputDevices, ref s_InputDeviceCount, i);
                    ////REVIEW: should this rather queue a device removal event?
                    InputSystem.RemoveDevice(device);
                    --i;
                    continue;
                }

                ////TODO: support polling Steam controllers on an async polling thread adhering to InputSystem.pollingFrequency
                // Otherwise, update it.
                device.InvokeUpdate();
            }
        }
    }
}

#endif // (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
