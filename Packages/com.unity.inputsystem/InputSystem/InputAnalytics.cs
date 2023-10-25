#if UNITY_ANALYTICS || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

////FIXME: apparently shutdown events are not coming through in the analytics backend

namespace UnityEngine.InputSystem
{
    internal static class InputAnalytics
    {
        public const string kEventStartup = "input_startup";
        public const string kEventShutdown = "input_shutdown";
        public const string kEventInputActionEditorWindowSession = "input_action_editor_window_session";

        public static void Initialize(InputManager manager)
        {
            Debug.Assert(manager.m_Runtime != null);
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
            data.new_enabled = EditorPlayerSettingHelpers.newSystemBackendsEnabled;
            data.old_enabled = EditorPlayerSettingHelpers.oldSystemBackendsEnabled;
            #endif

            manager.m_Runtime.RegisterAnalyticsEvent(kEventStartup, 100, 100);
            manager.m_Runtime.SendAnalyticsEvent(kEventStartup, data);
            
            #if UNITY_EDITOR
            manager.m_Runtime.RegisterAnalyticsEvent(name: kEventInputActionEditorWindowSession, maxPerHour: 100, maxPropertiesPerEvent: 100);
            #endif
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
                total_frame_count = metrics.totalUpdateCount,
                total_event_processing_time = (float)metrics.totalEventProcessingTime,
            };

            manager.m_Runtime.RegisterAnalyticsEvent(kEventShutdown, 10, 100);
            manager.m_Runtime.SendAnalyticsEvent(kEventShutdown, data);
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

        /// <summary>
        /// Represents an editor type.
        /// </summary>
        /// <remarks>
        /// This may be added to in the future but items may never be removed.
        /// </remarks>
        public enum InputActionsEditorType
        {
            Invalid = 0,
            FreeFloatingEditorWindow = 1,
            EmbeddedInProjectSettings = 2
        }

        /// <summary>
        /// Analytics record for tracking engagement with Input Action Asset editor(s).
        /// </summary>
        [Serializable]
        public struct InputActionsEditorSession
        {
            /// <summary>
            /// Construct a new <c>InputActionsEditorSession</c> record of the given <para>type</para>.
            /// </summary>
            /// <param name="type">The editor type for which this record is valid.</param>
            public InputActionsEditorSession(InputActionsEditorType type)
            {
                this.type = type;
                sessionDurationSeconds = 0;
                sessionFocusDurationSeconds = 0;
                sessionFocusSwitchCount = 0;
                actionMapModificationCount = 0;
                actionModificationCount = 0;
                bindingModificationCount = 0;
                explicitSaveCount = 0;
                autoSaveCount = 0;
                
                m_FocusStart = float.NaN;
                m_SessionStart = float.NaN;
            }

            /// <summary>
            /// Register that an action map edit has occurred.
            /// </summary>
            public void RegisterActionMapEdit()
            {
                if (hasSession && hasFocus)
                    ++actionMapModificationCount;
            }

            /// <summary>
            /// Register that an action edit has occurred.
            /// </summary>
            public void RegisterActionEdit()
            {
                if (hasSession && hasFocus)
                    ++actionModificationCount;
            }

            /// <summary>
            /// Register than a binding edit has occurred.
            /// </summary>
            public void RegisterBindingEdit()
            {
                if (hasSession && hasFocus)
                    ++bindingModificationCount;
            }

            /// <summary>
            /// Register that the editor has received focus which is expected to reflect that the user
            /// is currently exploring or editing it.
            /// </summary>
            public void RegisterEditorFocusIn()
            {
                if (!hasSession || hasFocus)
                    return;

                m_FocusStart = CurrentTime();
            }

            /// <summary>
            /// Register that the editor has lost focus which is expected to reflect that the user currently
            /// has the attention elsewhere.
            /// </summary>
            public void RegisterEditorFocusOut()
            {
                if (!hasSession || !hasFocus)
                    return;
                
                var duration = CurrentTime() - m_FocusStart;
                m_FocusStart = float.NaN;
                sessionFocusDurationSeconds += (float)duration;
                ++sessionFocusSwitchCount;
            }

            /// <summary>
            /// Begins a new session.
            /// </summary>
            public void Begin()
            {
                if (hasSession)
                    return;
                
                m_SessionStart = CurrentTime();
            }

            /// <summary>
            /// Ends the current session.
            /// </summary>
            public void End()
            {
                if (!hasSession)
                    return;
                
                var duration = CurrentTime() - m_SessionStart;
                this.sessionDurationSeconds += (float)duration;
            }
            
            public override string ToString()
            {
                return $"{nameof(type)}: {type}, " +
                       $"{nameof(sessionDurationSeconds)}: {sessionDurationSeconds} seconds, " +
                       $"{nameof(sessionFocusDurationSeconds)}: {sessionFocusDurationSeconds} seconds, " +
                       $"{nameof(sessionFocusSwitchCount)}: {sessionFocusSwitchCount}, " +
                       $"{nameof(actionMapModificationCount)}: {actionMapModificationCount}, " +
                       $"{nameof(actionModificationCount)}: {actionModificationCount}, " +
                       $"{nameof(bindingModificationCount)}: {bindingModificationCount}, " +
                       $"{nameof(explicitSaveCount)}: {explicitSaveCount}, " +
                       $"{nameof(autoSaveCount)}: {autoSaveCount}";
            }

            private InputActionsEditorType type;
            private float sessionDurationSeconds;
            private float sessionFocusDurationSeconds;
            private int sessionFocusSwitchCount;
            private int actionMapModificationCount;
            private int actionModificationCount;
            private int bindingModificationCount;
            private int explicitSaveCount;
            private int autoSaveCount;
            
            [NonSerialized] private double m_FocusStart;
            [NonSerialized] private double m_SessionStart;

            private bool hasFocus => !double.IsNaN(m_FocusStart);
            private bool hasSession => !double.IsNaN(m_SessionStart);
            private double CurrentTime() => EditorApplication.timeSinceStartup;
            public bool isValid => sessionDurationSeconds >= 0;
        }

        /// <summary>
        /// Reports analytics for a single Input Actions Asset editing session.
        /// </summary>
        /// <param name="session">The session related analytics.</param>
        public static void OnInputActionEditorSessionEnd(ref InputActionsEditorSession session)
        {
            if (!session.isValid)
                return;
            
            Debug.Log("OnInputActionsEditorEndSession: " + session);
        }
    }
}
#endif // UNITY_ANALYTICS || UNITY_EDITOR
