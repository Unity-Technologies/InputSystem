#if UNITY_ANALYTICS || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Layouts;
#if UNITY_EDITOR
using UnityEngine.Experimental.Input.Editor;
#endif

namespace UnityEngine.Experimental.Input
{
    internal static class InputAnalytics
    {
        public const string kEventStartup = "input_startup";
        public const string kEventFirstUserInteraction = "input_first_user_interaction";
        public const string kEventShutdown = "input_shutdown";

        public static void Initialize(InputManager manager)
        {
            var runtime = manager.m_Runtime;
            Debug.Assert(runtime != null);

            // Register our analytics events. We want all of them to be pretty low volume.
            // All of them are per session. kEventStartup can even be per installation.
            runtime.RegisterAnalyticsEvent(kEventStartup, 10, 100);
            runtime.RegisterAnalyticsEvent(kEventFirstUserInteraction, 10, 100);
            runtime.RegisterAnalyticsEvent(kEventShutdown, 10, 100);
        }

        public static void OnStartup(InputManager manager)
        {
            var data = new StartupEventData
            {
                version = InputSystem.version.ToString(),
            };

            // Collect recognized devices.
            var devices = manager.devices;
            var deviceList = new List<StartupEventData.DeviceInfo>();
            for (var i = 0; i < devices.Count; ++i)
            {
                var device = devices[i];
                if (IsIgnoredDevice(device.description))
                    continue;

                deviceList.Add(
                    StartupEventData.DeviceInfo.FromDescription(device.description, device.native, device.layout));
            }
            data.devices = deviceList.ToArray();

            // Collect unrecognized devices.
            deviceList.Clear();
            var availableDevices = manager.m_AvailableDevices;
            var availableDeviceCount = manager.m_AvailableDeviceCount;
            for (var i = 0; i < availableDeviceCount; ++i)
            {
                var deviceId = availableDevices[i].deviceId;
                if (manager.TryGetDeviceById(deviceId) != null)
                    continue;

                deviceList.Add(StartupEventData.DeviceInfo.FromDescription(availableDevices[i].description,
                    availableDevices[i].isNative));
            }

            data.unrecognized_devices = deviceList.ToArray();

            #if UNITY_EDITOR
            data.new_enabled = EditorPlayerSettings.newSystemBackendsEnabled;
            data.old_enabled = EditorPlayerSettings.oldSystemBackendsEnabled;
            #endif

            manager.m_Runtime.SendAnalyticsEvent(kEventStartup, data);
        }

        public static void OnFirstUserInteraction(InputManager manager, double time, InputControl control)
        {
        }

        public static void OnShutdown(InputManager manager)
        {
            var metrics = manager.metrics;
            var data = new ShutdownEventData
            {
                max_num_devices = metrics.maxNumDevices,
                max_state_size_in_bytes = metrics.maxStateSizeInBytes,
                total_event_bytes = metrics.totalEventBytes,
                total_event_count = metrics.totalEventCount,
                total_frame_count = metrics.totalFrameCount,
                total_event_processing_time = (float)metrics.totalEventProcessingTime,
            };

            manager.m_Runtime.SendAnalyticsEvent(kEventShutdown, data);
        }

        private static bool IsIgnoredDevice(InputDeviceDescription description)
        {
            #if UNITY_STANDALONE_WIN
            #endif

            return false;
        }

        /// <summary>
        /// Data about what configuration we start up with.
        /// </summary>
        /// <remarks>
        /// Has data about the devices present at startup so that we can know what's being
        /// used out there. Also has data about devices we couldn't recognize.
        ///
        /// Note that we exclude devices that are always present (e.g. keyboard and mouse
        /// on desktops or touchscreen on phones).
        /// </remarks>
        [Serializable]
        public struct StartupEventData
        {
            public string version;
            public DeviceInfo[] devices;
            public DeviceInfo[] unrecognized_devices;

            ////REVIEW: ATM we have no way of retrieving these in the player
            #if UNITY_EDITOR
            public bool new_enabled;
            public bool old_enabled;
            #endif

            [Serializable]
            public struct DeviceInfo
            {
                public string layout;
                public string @interface;
                public string product;
                public bool native;

                public static DeviceInfo FromDescription(InputDeviceDescription description, bool native = false, string layout = null)
                {
                    string product;
                    if (!string.IsNullOrEmpty(description.product) && !string.IsNullOrEmpty(description.manufacturer))
                        product = string.Format("{0} {1}", description.manufacturer, description.product);
                    else if (!string.IsNullOrEmpty(description.product))
                        product = description.product;
                    else
                        product = description.manufacturer;

                    if (string.IsNullOrEmpty(layout))
                        layout = description.deviceClass;

                    return new DeviceInfo
                    {
                        layout = layout,
                        @interface = description.interfaceName,
                        product = product,
                        native = native
                    };
                }
            }
        }

        /// <summary>
        /// Data about when after startup the user first interacted with the application.
        /// </summary>
        [Serializable]
        public struct FirstUserInteractionEventData
        {
        }

        /// <summary>
        /// Data about what level of data we pumped through the system throughout its lifetime.
        /// </summary>
        [Serializable]
        public struct ShutdownEventData
        {
            public int max_num_devices;
            public int max_state_size_in_bytes;
            public int total_event_bytes;
            public int total_event_count;
            public int total_frame_count;
            public float total_event_processing_time;
        }
    }
}
#endif // UNITY_ANALYTICS || UNITY_EDITOR
