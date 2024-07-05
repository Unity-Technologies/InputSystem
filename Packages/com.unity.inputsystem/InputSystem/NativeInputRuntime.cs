using System;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Analytics;
using UnityEngine.InputSystem.Utilities;
using UnityEngineInternal.Input;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

#endif

// This should be the only file referencing the API at UnityEngineInternal.Input.

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Implements <see cref="IInputRuntime"/> based on <see cref="NativeInputSystem"/>.
    /// </summary>
    internal class NativeInputRuntime : IInputRuntime
    {
        public static readonly NativeInputRuntime instance = new NativeInputRuntime();

        public int AllocateDeviceId()
        {
            return NativeInputSystem.AllocateDeviceId();
        }

        public void Update(InputUpdateType updateType)
        {
            NativeInputSystem.Update((NativeInputUpdateType)updateType);
        }

        public unsafe void QueueEvent(InputEvent* ptr)
        {
            NativeInputSystem.QueueInputEvent((IntPtr)ptr);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "False positive.")]
        public unsafe long DeviceCommand(int deviceId, InputDeviceCommand* commandPtr)
        {
            if (commandPtr == null)
                throw new ArgumentNullException(nameof(commandPtr));

            return NativeInputSystem.IOCTL(deviceId, commandPtr->type, new IntPtr(commandPtr->payloadPtr), commandPtr->payloadSizeInBytes);
        }

        public unsafe InputUpdateDelegate onUpdate
        {
            get => m_OnUpdate;
            set
            {
                if (value != null)
                    NativeInputSystem.onUpdate =
                        (updateType, eventBufferPtr) =>
                    {
                        var buffer = new InputEventBuffer((InputEvent*)eventBufferPtr->eventBuffer,
                            eventBufferPtr->eventCount,
                            sizeInBytes: eventBufferPtr->sizeInBytes,
                            capacityInBytes: eventBufferPtr->capacityInBytes);

                        try
                        {
                            value((InputUpdateType)updateType, ref buffer);
                        }
                        catch (Exception e)
                        {
                            // Always report the original exception first to confuse users less about what it the actual failure.
                            Debug.LogException(e);
                            Debug.LogError($"{e.GetType().Name} during event processing of {updateType} update; resetting event buffer");
                            buffer.Reset();
                        }

                        if (buffer.eventCount > 0)
                        {
                            eventBufferPtr->eventCount = buffer.eventCount;
                            eventBufferPtr->sizeInBytes = (int)buffer.sizeInBytes;
                            eventBufferPtr->capacityInBytes = (int)buffer.capacityInBytes;
                            eventBufferPtr->eventBuffer =
                                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(buffer.data);
                        }
                        else
                        {
                            eventBufferPtr->eventCount = 0;
                            eventBufferPtr->sizeInBytes = 0;
                        }
                    };
                else
                    NativeInputSystem.onUpdate = null;
                m_OnUpdate = value;
            }
        }

        public Action<InputUpdateType> onBeforeUpdate
        {
            get => m_OnBeforeUpdate;
            set
            {
                // This is stupid but the enum prevents us from jacking the delegate in directly.
                // This means we get a double dispatch here :(
                if (value != null)
                    NativeInputSystem.onBeforeUpdate = updateType => value((InputUpdateType)updateType);
                else
                    NativeInputSystem.onBeforeUpdate = null;
                m_OnBeforeUpdate = value;
            }
        }

        public Func<InputUpdateType, bool> onShouldRunUpdate
        {
            get => m_OnShouldRunUpdate;
            set
            {
                // This is stupid but the enum prevents us from jacking the delegate in directly.
                // This means we get a double dispatch here :(
                if (value != null)
                    NativeInputSystem.onShouldRunUpdate = updateType => value((InputUpdateType)updateType);
                else
                    NativeInputSystem.onShouldRunUpdate = null;
                m_OnShouldRunUpdate = value;
            }
        }

        #if UNITY_EDITOR
        private struct InputSystemPlayerLoopRunnerInitializationSystem {};
        public Action onPlayerLoopInitialization
        {
            get => m_PlayerLoopInitialization;
            set
            {
                // This is a hot-fix for a critical problem in input system, case 1368559, case 1367556, case 1372830
                // TODO move it to a proper native callback instead
                if (value != null)
                {
                    // Inject ourselves directly to PlayerLoop.Initialization as first subsystem to run,
                    // Use InputSystemPlayerLoopRunnerInitializationSystem as system type
                    var playerLoop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
                    var initStepIndex = playerLoop.subSystemList.IndexOf(x => x.type == typeof(PlayerLoop.Initialization));
                    if (initStepIndex >= 0)
                    {
                        var systems = playerLoop.subSystemList[initStepIndex].subSystemList;

                        // Check if we're not already injected
                        if (!systems.Select(x => x.type)
                            .Contains(typeof(InputSystemPlayerLoopRunnerInitializationSystem)))
                        {
                            ArrayHelpers.InsertAt(ref systems, 0, new UnityEngine.LowLevel.PlayerLoopSystem
                            {
                                type = typeof(InputSystemPlayerLoopRunnerInitializationSystem),
                                updateDelegate = () => m_PlayerLoopInitialization?.Invoke()
                            });

                            playerLoop.subSystemList[initStepIndex].subSystemList = systems;
                            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(playerLoop);
                        }
                    }
                }

                m_PlayerLoopInitialization = value;
            }
        }
        #endif

        public Action<int, string> onDeviceDiscovered
        {
            get => NativeInputSystem.onDeviceDiscovered;
            set => NativeInputSystem.onDeviceDiscovered = value;
        }

        public Action onShutdown
        {
            get => m_ShutdownMethod;
            set
            {
                if (value == null)
                {
                    #if UNITY_EDITOR
                    EditorApplication.wantsToQuit -= OnWantsToShutdown;
                    #else
                    Application.quitting -= OnShutdown;
                    #endif
                }
                else if (m_ShutdownMethod == null)
                {
                    #if UNITY_EDITOR
                    EditorApplication.wantsToQuit += OnWantsToShutdown;
                    #else
                    Application.quitting += OnShutdown;
                    #endif
                }

                m_ShutdownMethod = value;
            }
        }

        public Action<bool> onPlayerFocusChanged
        {
            get => m_FocusChangedMethod;
            set
            {
                if (value == null)
                    Application.focusChanged -= OnFocusChanged;
                else if (m_FocusChangedMethod == null)
                    Application.focusChanged += OnFocusChanged;
                m_FocusChangedMethod = value;
            }
        }

        public bool isPlayerFocused => Application.isFocused;

        public float pollingFrequency
        {
            get => m_PollingFrequency;
            set
            {
                m_PollingFrequency = value;
                NativeInputSystem.SetPollingFrequency(value);
            }
        }

        public double currentTime => NativeInputSystem.currentTime;

        ////REVIEW: this applies the offset, currentTime doesn't
        public double currentTimeForFixedUpdate => Time.fixedUnscaledTime + currentTimeOffsetToRealtimeSinceStartup;

        public double currentTimeOffsetToRealtimeSinceStartup => NativeInputSystem.currentTimeOffsetToRealtimeSinceStartup;
        public float unscaledGameTime => Time.unscaledTime;

        public bool runInBackground
        {
            get =>
                Application.runInBackground ||
                // certain platforms ignore the runInBackground flag and always run. Make sure we're
                // not running on one of those and set the values when running on specific platforms.
                m_RunInBackground;
            set => m_RunInBackground = value;
        }

        bool m_RunInBackground;

        private Action m_ShutdownMethod;
        private InputUpdateDelegate m_OnUpdate;
        private Action<InputUpdateType> m_OnBeforeUpdate;
        private Func<InputUpdateType, bool> m_OnShouldRunUpdate;
        #if UNITY_EDITOR
        private Action m_PlayerLoopInitialization;
        #endif
        private float m_PollingFrequency = 60.0f;
        private bool m_DidCallOnShutdown = false;
        private void OnShutdown()
        {
            m_ShutdownMethod();
        }

        private bool OnWantsToShutdown()
        {
            if (!m_DidCallOnShutdown)
            {
                // we should use `EditorApplication.quitting`, but that is too late
                // to send an analytics event, because Analytics is already shut down
                // at that point. So we use `EditorApplication.wantsToQuit`, and make sure
                // to only use the first time. This is currently only used for analytics,
                // and getting analytics before we actually shut downn in some cases is
                // better then never.

                OnShutdown();
                m_DidCallOnShutdown = true;
            }

            return true;
        }

        private Action<bool> m_FocusChangedMethod;

        private void OnFocusChanged(bool focus)
        {
            m_FocusChangedMethod(focus);
        }

        public Vector2 screenSize => new Vector2(Screen.width, Screen.height);
        public ScreenOrientation screenOrientation => Screen.orientation;

#if UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA
        public bool normalizeScrollWheelDelta
        {
            get => NativeInputSystem.normalizeScrollWheelDelta;
            set => NativeInputSystem.normalizeScrollWheelDelta = value;
        }

        public float scrollWheelDeltaPerTick
        {
            get => NativeInputSystem.GetScrollWheelDeltaPerTick();
        }
#endif

        public bool isInBatchMode => Application.isBatchMode;

        #if UNITY_EDITOR

        public bool isInPlayMode => EditorApplication.isPlaying;
        public bool isPaused => EditorApplication.isPaused;
        public bool isEditorActive => InternalEditorUtility.isApplicationActive;

        public Func<IntPtr, bool> onUnityRemoteMessage
        {
            set
            {
                if (m_UnityRemoteMessageHandler == value)
                    return;

                if (m_UnityRemoteMessageHandler != null)
                {
                    var removeMethod = GetUnityRemoteAPIMethod("RemoveMessageHandler");
                    removeMethod?.Invoke(null, new[] { m_UnityRemoteMessageHandler });
                    m_UnityRemoteMessageHandler = null;
                }

                if (value != null)
                {
                    var addMethod = GetUnityRemoteAPIMethod("AddMessageHandler");
                    addMethod?.Invoke(null, new[] { value });
                    m_UnityRemoteMessageHandler = value;
                }
            }
        }

        public void SetUnityRemoteGyroEnabled(bool value)
        {
            var setMethod = GetUnityRemoteAPIMethod("SetGyroEnabled");
            setMethod?.Invoke(null, new object[] { value });
        }

        public void SetUnityRemoteGyroUpdateInterval(float interval)
        {
            var setMethod = GetUnityRemoteAPIMethod("SetGyroUpdateInterval");
            setMethod?.Invoke(null, new object[] { interval });
        }

        private MethodInfo GetUnityRemoteAPIMethod(string methodName)
        {
            var editorAssembly = typeof(EditorApplication).Assembly;
            var genericRemoteClass = editorAssembly.GetType("UnityEditor.Remote.GenericRemote");
            if (genericRemoteClass == null)
                return null;

            return genericRemoteClass.GetMethod(methodName);
        }

        private Func<IntPtr, bool> m_UnityRemoteMessageHandler;
        private Action<PlayModeStateChange> m_OnPlayModeChanged;
        private Action m_OnProjectChanged;

        private void OnPlayModeStateChanged(PlayModeStateChange value)
        {
            m_OnPlayModeChanged(value);
        }

        private void OnProjectChanged()
        {
            m_OnProjectChanged();
        }

        public Action<PlayModeStateChange> onPlayModeChanged
        {
            get => m_OnPlayModeChanged;
            set
            {
                if (value == null)
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                else if (m_OnPlayModeChanged == null)
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                m_OnPlayModeChanged = value;
            }
        }

        public Action onProjectChange
        {
            get => m_OnProjectChanged;
            set
            {
                if (value == null)
                    EditorApplication.projectChanged -= OnProjectChanged;
                else if (m_OnProjectChanged == null)
                    EditorApplication.projectChanged += OnProjectChanged;
                m_OnProjectChanged = value;
            }
        }

        #endif // UNITY_EDITOR

        #if UNITY_ANALYTICS || UNITY_EDITOR

        public void SendAnalytic(InputAnalytics.IInputAnalytic analytic)
        {
            #if (UNITY_EDITOR)
            #if (UNITY_2023_2_OR_NEWER)
            EditorAnalytics.SendAnalytic(analytic);
            #else
            var info = analytic.info;
            EditorAnalytics.RegisterEventWithLimit(info.Name, info.MaxEventsPerHour, info.MaxNumberOfElements, InputAnalytics.kVendorKey);
            EditorAnalytics.SendEventWithLimit(info.Name, analytic);
            #endif // UNITY_2023_2_OR_NEWER
            #elif UNITY_ANALYTICS // Implicitly: !UNITY_EDITOR
            var info = analytic.info;
            Analytics.Analytics.RegisterEvent(info.Name, info.MaxEventsPerHour, info.MaxNumberOfElements, InputAnalytics.kVendorKey);
            if (analytic.TryGatherData(out var data, out var error))
                Analytics.Analytics.SendEvent(info.Name, data);
            else
                Debug.Log(error); // Non fatal
            #endif // UNITY_EDITOR
        }

        #endif // UNITY_ANALYTICS || UNITY_EDITOR
    }
}
