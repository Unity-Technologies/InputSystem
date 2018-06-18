#if UNITY_ANALYTICS || UNITY_EDITOR
using System;

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
        }

        public static void OnFirstUserInteraction(double time, InputControl control)
        {
        }

        public static void OnShutdown(ref InputMetrics metrics)
        {
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
            public string[] unrecognizedDevices;

            ////REVIEW: ATM we have no way of retrieving these in the player
            #if UNITY_EDITOR
            public bool new_enabled;
            public bool old_enabled;
            #endif

            [Serializable]
            public struct DeviceInfo
            {
                public string layout;
                public string type;
                public string description;
                public bool native;
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
        }
    }
}
#endif // UNITY_ANALYTICS || UNITY_EDITOR
