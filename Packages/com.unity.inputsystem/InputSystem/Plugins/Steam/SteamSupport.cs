#if (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using System;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Steam
{
    /// <summary>
    /// Adds support for Steam controllers.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class SteamSupport
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
                InstallHooks(s_API != null);
            }
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
        internal static bool s_HooksInstalled;
        internal static ISteamControllerAPI s_API;

        private const int STEAM_CONTROLLER_MAX_COUNT = 16;

        /// <summary>
        /// Enable support for the Steam controller API.
        /// </summary>
        public static void Initialize()
        {
            // We use this as a base layout.
            InputSystem.RegisterLayout<SteamController>();

            if (api != null)
                InstallHooks(true);
        }

        private static void InstallHooks(bool state)
        {
            Debug.Assert(api != null);
            if (state && !s_HooksInstalled)
            {
                InputSystem.onBeforeUpdate += OnUpdate;
                InputSystem.onActionChange += OnActionChange;
            }
            else if (!state && s_HooksInstalled)
            {
                InputSystem.onBeforeUpdate -= OnUpdate;
                InputSystem.onActionChange -= OnActionChange;
            }
        }

        private static void OnActionChange(object mapOrAction, InputActionChange change)
        {
            // We only care about action map activations. Steam has no support for enabling or disabling
            // individual actions and also has no support disabling sets once enabled (can only switch
            // to different set).
            if (change != InputActionChange.ActionMapEnabled)
                return;

            // See if the map has any bindings to SteamControllers.
            // NOTE: We only support a single SteamController on any action map here. The first SteamController
            //       we find is the one we're doing all the work on.
            var actionMap = (InputActionMap)mapOrAction;
            foreach (var action in actionMap.actions)
            {
                foreach (var control in action.controls)
                {
                    var steamController = control.device as SteamController;
                    if (steamController == null)
                        continue;

                    // Yes, there's active bindings to a SteamController on the map. Look through the Steam action
                    // sets on the controller for a name match on the action map. If we have one, sync the enable/
                    // disable status of the set.
                    var actionMapName = actionMap.name;
                    foreach (var set in steamController.steamActionSets)
                    {
                        if (string.Compare(set.name, actionMapName, StringComparison.InvariantCultureIgnoreCase) != 0)
                            continue;

                        // Nothing to do if the Steam controller has auto-syncing disabled.
                        if (!steamController.autoActivateSets)
                            return;

                        // Sync status.
                        steamController.ActivateSteamActionSet(set.handle);

                        // Done.
                        return;
                    }
                }
            }
        }

        private static void OnUpdate()
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
                        if (s_InputDevices[n].steamControllerHandle == handle)
                        {
                            existingDevice = s_InputDevices[n];
                            break;
                        }
                    }

                    // Yes, we do.
                    if (existingDevice != null)
                        continue;
                }

                ////FIXME: this should not create garbage
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
                    steamDevice.InvokeResolveSteamActions();

                    // Assign it the Steam controller handle.
                    steamDevice.steamControllerHandle = handle;

                    ArrayHelpers.AppendWithCapacity(ref s_InputDevices, ref s_InputDeviceCount, steamDevice);
                }
            }

            // Update all controllers we have.
            for (var i = 0; i < s_InputDeviceCount; ++i)
            {
                var device = s_InputDevices[i];
                var handle = device.steamControllerHandle;

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
