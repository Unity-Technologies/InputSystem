#if UNITY_ANALYTICS || UNITY_EDITOR
using System;
using UnityEngine.InputSystem.Layouts;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif // UNITY_EDITOR

////FIXME: apparently shutdown events are not coming through in the analytics backend

namespace UnityEngine.InputSystem
{
    internal static class InputAnalytics
    {
        public const string kVendorKey = "unity.input";

        // Struct similar to AnalyticInfo for simplifying usage.
        public struct InputAnalyticInfo
        {
            public InputAnalyticInfo(string name, int maxEventsPerHour, int maxNumberOfElements)
            {
                Name = name;
                MaxEventsPerHour = maxEventsPerHour;
                MaxNumberOfElements = maxNumberOfElements;
            }

            public readonly string Name;
            public readonly int MaxEventsPerHour;
            public readonly int MaxNumberOfElements;
        }

        // Note: Needs to be externalized from interface depending on C# version.
        public interface IInputAnalyticData
#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            : UnityEngine.Analytics.IAnalytic.IData
#endif
        {}

        // Unity 2023.2+ deprecates legacy interfaces for registering and sending editor analytics and
        // replaces them with attribute annotations and required interface implementations.
        // The IInputAnalytic interface have been introduced here to support both variants
        // of analytics reporting. Notice that a difference is that data is collected lazily as part
        // of sending the analytics via the framework.
        public interface IInputAnalytic
#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            : UnityEngine.Analytics.IAnalytic
#endif // UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        {
            InputAnalyticInfo info { get; } // May be removed when only supporting 2023.2+ versions

#if !UNITY_2023_2_OR_NEWER
            // Conditionally mimic UnityEngine.Analytics.IAnalytic
            bool TryGatherData(out IInputAnalyticData data, out Exception error);
#endif // !UNITY_2023_2_OR_NEWER
        }

        public static void Initialize(InputManager manager)
        {
            Debug.Assert(manager.m_Runtime != null);
        }

        public static void OnStartup(InputManager manager)
        {
            manager.m_Runtime.SendAnalytic(new StartupEventAnalytic(manager));
        }

        public static void OnShutdown(InputManager manager)
        {
            manager.m_Runtime.SendAnalytic(new ShutdownEventDataAnalytic(manager));
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
        public struct StartupEventData : IInputAnalyticData
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
                        product = $"{description.manufacturer} {description.product}";
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

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour, maxNumberOfElements: kMaxNumberOfElements, vendorKey: kVendorKey)]
#endif // UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        public struct StartupEventAnalytic : IInputAnalytic
        {
            public const string kEventName = "input_startup";
            public const int kMaxEventsPerHour = 100;
            public const int kMaxNumberOfElements = 100;

            private InputManager m_InputManager;

            public StartupEventAnalytic(InputManager manager)
            {
                m_InputManager = manager;
            }

            public InputAnalyticInfo info => new InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
            public bool TryGatherData(out IInputAnalyticData data, out Exception error)
#endif
            {
                try
                {
                    data = new StartupEventData
                    {
                        version = InputSystem.version.ToString(),
                        devices = CollectRecognizedDevices(m_InputManager),
                        unrecognized_devices = CollectUnrecognizedDevices(m_InputManager),
#if UNITY_EDITOR
                        new_enabled = EditorPlayerSettingHelpers.newSystemBackendsEnabled,
                        old_enabled = EditorPlayerSettingHelpers.oldSystemBackendsEnabled,
#endif // UNITY_EDITOR
                    };
                    error = null;
                    return true;
                }
                catch (Exception e)
                {
                    data = null;
                    error = e;
                    return false;
                }
            }

            private static StartupEventData.DeviceInfo[] CollectRecognizedDevices(InputManager manager)
            {
                var deviceInfo = new StartupEventData.DeviceInfo[manager.devices.Count];
                for (var i = 0; i < manager.devices.Count; ++i)
                {
                    deviceInfo[i] = StartupEventData.DeviceInfo.FromDescription(
                        manager.devices[i].description, manager.devices[i].native, manager.devices[i].layout);
                }
                return deviceInfo;
            }

            private static StartupEventData.DeviceInfo[] CollectUnrecognizedDevices(InputManager manager)
            {
                var n = 0;
                var deviceInfo = new StartupEventData.DeviceInfo[manager.m_AvailableDeviceCount];
                for (var i = 0; i < deviceInfo.Length; ++i)
                {
                    var deviceId = manager.m_AvailableDevices[i].deviceId;
                    if (manager.TryGetDeviceById(deviceId) != null)
                        continue;

                    deviceInfo[n++] = StartupEventData.DeviceInfo.FromDescription(
                        manager.m_AvailableDevices[i].description, manager.m_AvailableDevices[i].isNative);
                }

                if (deviceInfo.Length > n)
                    Array.Resize(ref deviceInfo, n);

                return deviceInfo;
            }
        }

        /// <summary>
        /// Data about when after startup the user first interacted with the application.
        /// </summary>
        [Serializable]
        public struct FirstUserInteractionEventData : IInputAnalyticData
        {
        }

        /// <summary>
        /// Data about what level of data we pumped through the system throughout its lifetime.
        /// </summary>
        [Serializable]
        public struct ShutdownEventData : IInputAnalyticData
        {
            public int max_num_devices;
            public int max_state_size_in_bytes;
            public int total_event_bytes;
            public int total_event_count;
            public int total_frame_count;
            public float total_event_processing_time;
        }

#if (UNITY_EDITOR && UNITY_2023_2_OR_NEWER)
        [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
            maxNumberOfElements: kMaxNumberOfElements, vendorKey: kVendorKey)]
#endif // (UNITY_EDITOR && UNITY_2023_2_OR_NEWER)
        public readonly struct ShutdownEventDataAnalytic : IInputAnalytic
        {
            public const string kEventName = "input_shutdown";
            public const int kMaxEventsPerHour = 100;
            public const int kMaxNumberOfElements = 100;

            private readonly InputManager m_InputManager;

            public ShutdownEventDataAnalytic(InputManager manager)
            {
                m_InputManager = manager;
            }

            public InputAnalyticInfo info => new InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
            public bool TryGatherData(out IInputAnalyticData data, out Exception error)
#endif
            {
                try
                {
                    var metrics = m_InputManager.metrics;
                    data = new ShutdownEventData
                    {
                        max_num_devices = metrics.maxNumDevices,
                        max_state_size_in_bytes = metrics.maxStateSizeInBytes,
                        total_event_bytes = metrics.totalEventBytes,
                        total_event_count = metrics.totalEventCount,
                        total_frame_count = metrics.totalUpdateCount,
                        total_event_processing_time = (float)metrics.totalEventProcessingTime,
                    };
                    error = null;
                    return true;
                }
                catch (Exception e)
                {
                    data = null;
                    error = e;
                    return false;
                }
            }
        }
    }

    internal static class AnalyticExtensions
    {
        internal static void Send<TSource>(this TSource analytic) where TSource : InputAnalytics.IInputAnalytic
        {
            InputSystem.s_Manager?.m_Runtime?.SendAnalytic(analytic);
        }
    }
}

#endif // UNITY_ANALYTICS || UNITY_EDITOR
