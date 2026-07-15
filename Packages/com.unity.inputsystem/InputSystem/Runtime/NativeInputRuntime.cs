using System;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif
using UnityEngine.InputSystem.Utilities;
using UnityEngineInternal.Input;

// This should be the only file referencing the API at UnityEngineInternal.Input.

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Implements <see cref="IInputRuntime"/> based on <see cref="NativeInputSystem"/>.
    /// </summary>
    internal class NativeInputRuntime : IInputRuntime
    {
        private static NativeInputRuntime s_Instance;

        // Private ctor exists to enforce Singleton pattern
        private NativeInputRuntime() {}

        /// <summary>
        /// Employ the Singleton pattern for this class and initialize a new instance on first use.
        /// </summary>
        /// <remarks>
        /// This property is typically used to initialize InputManager and isn't used afterwards, i.e. there's
        /// no perf impact to the null check.
        /// </remarks>
        public static NativeInputRuntime instance
        {
            get
            {
                s_Instance ??= new NativeInputRuntime();
                return s_Instance;
            }
        }

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
                            // Always report the original exception first so users can easily identify the actual failure.
                            Debug.LogException(e);
                            Debug.LogError($"Exception {e.GetType().Name}: {e.Message} during event processing of {updateType} update; resetting event buffer");
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

        // Callbacks set by Editor to handle shutdown subscription
        // In Editor, we use EditorApplication.wantsToQuit which expects Func<bool>
        internal Action<Func<bool>> m_RegisterWantsToQuit;
        internal Action<Func<bool>> m_UnregisterWantsToQuit;

        public Action onShutdown
        {
            get => m_ShutdownMethod;
            set
            {
                if (value == null)
                {
                    if (m_UnregisterWantsToQuit != null)
                    {
                        m_UnregisterWantsToQuit(OnWantsToShutdown);
                    }
                    else
                    {
                        Application.quitting -= OnShutdown;
                    }
                }
                else if (m_ShutdownMethod == null)
                {
                    if (m_RegisterWantsToQuit != null)
                    {
                        m_RegisterWantsToQuit(OnWantsToShutdown);
                    }
                    else
                    {
                        Application.quitting += OnShutdown;
                    }
                }

                m_ShutdownMethod = value;
            }
        }

#if !UNITY_INPUTSYSTEM_SUPPORTS_FOCUS_EVENTS
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
#endif

        private FocusFlags m_FocusState = FocusFlags.None;
        public FocusFlags focusState
        {
            get => m_FocusState;
            set => m_FocusState = value;
        }
        public bool isPlayerFocused => (m_FocusState & FocusFlags.ApplicationFocus) != FocusFlags.None;

        public float pollingFrequency
        {
            #if UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY
            get => NativeInputSystem.GetPollingFrequency();
            #else
            get => m_PollingFrequency;
            #endif
            set
            {
                #if !UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY
                m_PollingFrequency = value;
                #endif
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

        private bool m_RunInBackground;

        private Action m_ShutdownMethod;
        private InputUpdateDelegate m_OnUpdate;
        private Action<InputUpdateType> m_OnBeforeUpdate;
        private Func<InputUpdateType, bool> m_OnShouldRunUpdate;
        #if UNITY_EDITOR
        private Action m_PlayerLoopInitialization;
        #endif
        #if !UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY
        // From Unity 6000.3.0a2 (TODO Update comment and manifest before landing PR) this is handled by module
        // and initial value is suggested by the platform based on its supported device set.
        // In older version this is stored here and package override module/platform.
        private float m_PollingFrequency = 60.0f;
        #endif
        private bool m_DidCallOnShutdown;
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
                // and getting analytics before we actually shut down in some cases is
                // better than never.

                OnShutdown();
                m_DidCallOnShutdown = true;
            }

            return true;
        }

        public void InitializeFocusState()
        {
            m_FocusState = Application.isFocused
                ? m_FocusState | FocusFlags.ApplicationFocus
                : m_FocusState & ~FocusFlags.ApplicationFocus;
        }

#if !UNITY_INPUTSYSTEM_SUPPORTS_FOCUS_EVENTS
        private Action<bool> m_FocusChangedMethod;

        private void OnFocusChanged(bool focus)
        {
            m_FocusState = focus
                ? m_FocusState | FocusFlags.ApplicationFocus
                : m_FocusState & ~FocusFlags.ApplicationFocus;

            m_FocusChangedMethod(focus);
        }

#endif

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

        #if UNITY_EDITOR

        // These delegates are set by InputSystemEditorInitializer to avoid direct Editor dependencies
        internal Func<bool> m_IsInPlayMode;
        internal Func<bool> m_IsEditorActive;
        internal Func<bool> m_IsEditorPaused;

        public bool isInPlayMode => m_IsInPlayMode?.Invoke() ?? false;
        public bool isEditorActive => m_IsEditorActive?.Invoke() ?? true;
        public bool isEditorPaused => m_IsEditorPaused?.Invoke() ?? false;

        private Action<InputPlayModeChange> m_OnPlayModeChanged;
        private Action m_OnProjectChanged;
        /// <summary>
        /// Callback for play mode state changes.
        /// </summary>
        public Action<InputPlayModeChange> onPlayModeChanged
        {
            get => m_OnPlayModeChanged;
            set => m_OnPlayModeChanged = value;
        }

        public Action onProjectChange
        {
            get => m_OnProjectChanged;
            set => m_OnProjectChanged = value;
        }

        /// <summary>
        /// Called by InputSystemEditorInitializer to dispatch play mode changes
        /// </summary>
        internal void DispatchPlayModeChange(InputPlayModeChange change)
        {
            m_OnPlayModeChanged?.Invoke(change);
        }

        /// <summary>
        /// Called by InputSystemEditorInitializer to dispatch project changes
        /// </summary>
        internal void DispatchProjectChange()
        {
            m_OnProjectChanged?.Invoke();
        }

        #endif // UNITY_EDITOR

        #if UNITY_ANALYTICS || UNITY_EDITOR

        // Callback for sending analytics in Editor - set by InputSystemEditorInitializer
        internal Action<InputAnalytics.IInputAnalytic> m_SendEditorAnalytic;

        public void SendAnalytic(InputAnalytics.IInputAnalytic analytic)
        {
        #if ENABLE_CLOUD_SERVICES_ANALYTICS
            // In Editor, use the callback set by InputSystemEditorInitializer
            if (m_SendEditorAnalytic != null)
            {
                m_SendEditorAnalytic(analytic);
                return;
            }

            #if UNITY_ANALYTICS
            // In Player builds, use the regular Analytics API
            var info = analytic.info;
            Analytics.Analytics.RegisterEvent(info.Name, info.MaxEventsPerHour, info.MaxNumberOfElements, InputAnalytics.kVendorKey);
            if (analytic.TryGatherData(out var data, out var error))
                Analytics.Analytics.SendEvent(info.Name, data);
            else
                Debug.Log(error);     // Non fatal
            #endif
        #endif //ENABLE_CLOUD_SERVICES_ANALYTICS
        }

        #endif // UNITY_ANALYTICS || UNITY_EDITOR
    }
}
