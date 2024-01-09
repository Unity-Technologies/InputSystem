#if UNITY_ANALYTICS || UNITY_EDITOR
using System;
using UnityEngine.Analytics;
using UnityEngine.InputSystem.Layouts;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.LowLevel;
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

        // Note: Needs to be externalized from interface depending on C# version
        public interface IInputAnalyticData 
#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            : IInputAnalytic.IData
#endif
        {
            // Empty
        }
        
        // Unity 2023.2+ deprecates legacy interfaces for registering and sending editor analytics and
        // replaces them with attribute annotations and required interface implementations.
        // The IInputAnalytic interface have been introduced here to simplify supporting both variants
        // of analytics reporting. Notice that a difference is that data is collected lazily as part
        // of sending the analytics via the framework.
        public interface IInputAnalytic 
#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            : IAnalytic
#endif // UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        {
            InputAnalyticInfo info { get; } // May be removed when only supporting 2023.2+ versions
            
#if !UNITY_2023_2_OR_NEWER
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
        [AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour, maxNumberOfElements: kMaxNumberOfElements, vendorKey: kVendorKey)]
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

            public bool TryGatherData(out IInputAnalyticData data, out Exception error)
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
        [AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour, 
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
            
            public bool TryGatherData(out IInputAnalyticData data, out Exception error)
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

#if UNITY_EDITOR
        /// <summary>
        /// Represents an editor type.
        /// </summary>
        /// <remarks>
        /// This may be added to in the future but items may never be removed.
        /// </remarks>
        public enum InputActionsEditorKind
        {
            Invalid = 0,
            FreeFloatingEditorWindow = 1,
            EmbeddedInProjectSettings = 2
        }
        
        [Serializable]
        public struct InputActionsEditorSessionData : IInputAnalyticData
        {
            /// <summary>
            /// Constructs a <c>InputActionsEditorSessionData</c>.
            /// </summary>
            /// <param name="kind">Specifies the kind of editor metrics is being collected for.</param>
            public InputActionsEditorSessionData(InputActionsEditorKind kind)
            {
                this.kind = kind;
                sessionDurationSeconds = 0;
                sessionFocusDurationSeconds = 0;
                sessionFocusDurationSeconds = 0;
                sessionFocusSwitchCount = 0;
                actionMapModificationCount = 0;
                actionModificationCount = 0;
                bindingModificationCount = 0;
                explicitSaveCount = 0;
                autoSaveCount = 0;
                resetCount = 0;
                controlSchemeModificationCount = 0;
            }
            
            /// <summary>
            /// Specifies what kind of Input Actions editor this event represents.
            /// </summary>
            public InputActionsEditorKind kind;
            
            /// <summary>
            /// The total duration for the session, i.e. the duration during which the editor window was open.
            /// </summary>
            public float sessionDurationSeconds;
            
            /// <summary>
            /// The total duration for which the editor window was open and had focus.
            /// </summary>
            public float sessionFocusDurationSeconds;
            
            /// <summary>
            /// Specifies the number of times the window has transitioned from not having focus to having focus in a single session.  
            /// </summary>
            public int sessionFocusSwitchCount;
            
            /// <summary>
            /// The total number of action map modifications during the session.
            /// </summary>
            public int actionMapModificationCount;
            
            /// <summary>
            /// The total number of action modifications during the session.
            /// </summary>
            public int actionModificationCount;
            
            /// <summary>
            /// The total number of binding modifications during the session.
            /// </summary>
            public int bindingModificationCount;
            
            /// <summary>
            /// The total number of controls scheme modifications during the session.
            /// </summary>
            public int controlSchemeModificationCount;
            
            /// <summary>
            /// The total number of explicit saves during the session, i.e. as in user-initiated save.
            /// </summary>
            public int explicitSaveCount;
            
            /// <summary>
            /// The total number of automatic saves during the session, i.e. as in auto-save on close or focus-lost.
            /// </summary>
            public int autoSaveCount;

            /// <summary>
            /// The total number of user-initiated resets during the session, i.e. as in using Reset option in menu.
            /// </summary>
            public int resetCount;

            public bool isValid => kind != InputActionsEditorKind.Invalid && sessionDurationSeconds >= 0;
            
            public override string ToString()
            {
                return $"{nameof(kind)}: {kind}, " +
                       $"{nameof(sessionDurationSeconds)}: {sessionDurationSeconds} seconds, " +
                       $"{nameof(sessionFocusDurationSeconds)}: {sessionFocusDurationSeconds} seconds, " +
                       $"{nameof(sessionFocusSwitchCount)}: {sessionFocusSwitchCount}, " +
                       $"{nameof(actionMapModificationCount)}: {actionMapModificationCount}, " +
                       $"{nameof(actionModificationCount)}: {actionModificationCount}, " +
                       $"{nameof(bindingModificationCount)}: {bindingModificationCount}, " +
                       $"{nameof(controlSchemeModificationCount)}: {controlSchemeModificationCount}, " +
                       $"{nameof(explicitSaveCount)}: {explicitSaveCount}, " +
                       $"{nameof(autoSaveCount)}: {autoSaveCount}" +
                       $"{nameof(resetCount)}: {resetCount}";
            }
        }
        
        /// <summary>
        /// Analytics record for tracking engagement with Input Action Asset editor(s).
        /// </summary>
#if UNITY_2023_2_OR_NEWER    
        [AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour, 
            maxNumberOfElements: kMaxNumberOfElements, vendorKey: kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
        public class InputActionsEditorSessionAnalytic : IInputAnalytic
        {
            public const string kEventName = "inputActionEditorWindowSession";
            public const int kMaxEventsPerHour = 100; // default: 1000
            public const int kMaxNumberOfElements = 100; // default: 1000
            
            /// <summary>
            /// Construct a new <c>InputActionsEditorSession</c> record of the given <para>type</para>.
            /// </summary>
            /// <param name="kind">The editor type for which this record is valid.</param>
            public InputActionsEditorSessionAnalytic(InputActionsEditorKind kind)
            {
                if (kind == InputActionsEditorKind.Invalid)
                    throw new ArgumentException(nameof(kind));
                
                Initialize(kind);
            }

            /// <summary>
            /// Register that an action map edit has occurred.
            /// </summary>
            public void RegisterActionMapEdit()
            {
                if (ImplicitFocus())
                    ++m_Data.actionMapModificationCount;
            }

            /// <summary>
            /// Register that an action edit has occurred.
            /// </summary>
            public void RegisterActionEdit()
            {
                if (ImplicitFocus())
                    ++m_Data.actionModificationCount;
            }

            /// <summary>
            /// Register than a binding edit has occurred.
            /// </summary>
            public void RegisterBindingEdit()
            {
                if (ImplicitFocus())    
                    ++m_Data.bindingModificationCount;
            }

            public void RegisterControlSchemeEdit()
            {
                if (ImplicitFocus())
                    ++m_Data.controlSchemeModificationCount;
            }

            /// <summary>
            /// Register that the editor has received focus which is expected to reflect that the user
            /// is currently exploring or editing it.
            /// </summary>
            public void RegisterEditorFocusIn()
            {
                if (!hasSession || hasFocus)
                    return;

                m_FocusStart = currentTime;
            }

            /// <summary>
            /// Register that the editor has lost focus which is expected to reflect that the user currently
            /// has the attention elsewhere.
            /// </summary>
            /// <remarks>
            /// Calling this method without having an ongoing session and having focus will not have any effect.
            /// </remarks>
            public void RegisterEditorFocusOut()
            {
                if (!hasSession || !hasFocus)
                    return;
                
                var duration = currentTime - m_FocusStart;
                m_FocusStart = float.NaN;
                m_Data.sessionFocusDurationSeconds += (float)duration;
                ++m_Data.sessionFocusSwitchCount;
            }
            
            /// <summary>
            /// Register a user-event related to explicitly saving in the editor, e.g.
            /// using a button, menu or short-cut to trigger the save command.
            /// </summary>
            public void RegisterExplicitSave()
            {
                if (!hasSession)
                    return; // No pending session
                
                ++m_Data.explicitSaveCount;
            }

            /// <summary>
            /// Register a user-event related to implicitly saving in the editor, e.g.
            /// by having auto-save enabled and indirectly saving the associated asset.
            /// </summary>
            public void RegisterAutoSave()
            {
                if (!hasSession)
                    return; // No pending session

                ++m_Data.autoSaveCount;
            }

            /// <summary>
            /// Register a user-event related to resetting the editor action configuration to defaults.
            /// </summary>
            public void RegisterReset()
            {
                if (!hasSession)
                    return; // No pending session

                ++m_Data.resetCount;
            }

            /// <summary>
            /// Begins a new session if the session has not already been started.
            /// </summary>
            /// <remarks>
            /// If the session has already been started due to a previous call to <see cref="Begin()"/> without
            /// a call to <see cref="End()"/> this method has no effect.
            /// </remarks>
            public void Begin()
            {
                if (hasSession)
                    return; // Session already started.
                
                m_SessionStart = currentTime;
            }

            /// <summary>
            /// Ends the current session.
            /// </summary>
            /// <remarks>
            /// If the session has not previously been started via a call to <see cref="Begin()"/> calling this
            /// method has no effect.
            /// </remarks>
            public void End()
            {
                if (!hasSession)
                    return; // No pending session
                
                // Make sure we register focus out if failed to capture or not invoked
                if (hasFocus)
                    RegisterEditorFocusOut();
                
                // Compute and record total session duration
                var duration = currentTime - m_SessionStart;
                m_Data.sessionDurationSeconds += (float)duration;
                
                // Send analytics event
                runtime.SendAnalytic(this);
                
                // Reset to allow instance to be reused
                Initialize(m_Data.kind);
            }
            
            public bool TryGatherData(out IInputAnalyticData data, out Exception error)
            {
                if (!isValid)
                {
                    data = null;
                    error = new Exception("Unable to gather data without a valid session");
                    return false;
                }
                
                data = this.m_Data;
                error = null;
                return true; 
            }
            
            private void Initialize(InputActionsEditorKind kind)
            {
                m_FocusStart = float.NaN;
                m_SessionStart = float.NaN;
                
                m_Data = new InputActionsEditorSessionData(kind);
            }

            private bool ImplicitFocus()
            {
                if (!hasSession)
                    return false;
                if (!hasFocus)
                    RegisterEditorFocusIn();
                return true;
            }

            private readonly IInputRuntime m_InputRuntime;
            private InputActionsEditorSessionData m_Data;
            private double m_FocusStart;
            private double m_SessionStart;
            
            private IInputRuntime runtime => InputSystem.s_Manager.m_Runtime;
            private bool hasFocus => !double.IsNaN(m_FocusStart);
            private bool hasSession => !double.IsNaN(m_SessionStart);
            // Returns current time since startup. Note that IInputRuntime explicitly defines in interface that
            // IInputRuntime.currentTime corresponds to EditorApplication.timeSinceStartup in editor.
            private double currentTime => runtime.currentTime;
            public bool isValid => m_Data.sessionDurationSeconds >= 0;
            public InputAnalyticInfo info => new InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);
        }
    }
    
#endif // UNITY_EDITOR    
    
}

#endif // UNITY_ANALYTICS || UNITY_EDITOR
