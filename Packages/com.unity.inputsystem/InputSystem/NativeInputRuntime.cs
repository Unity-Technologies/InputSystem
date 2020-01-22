using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngineInternal.Input;

#if UNITY_EDITOR
using UnityEditor;
#endif

// This should be the only file referencing the API at UnityEngineInternal.Input.

#if !UNITY_2019_2_OR_NEWER
// The NativeInputSystem APIs are marked obsolete in 19.1, because they are becoming internal in 19.2
#pragma warning disable 618
#endif
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
                throw new System.ArgumentNullException(nameof(commandPtr));

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
                            Debug.LogError($"{e.GetType().Name} during event processing of {updateType} update; resetting event buffer");
                            Debug.LogException(e);
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

        public bool runInBackground => Application.runInBackground;

        private Action m_ShutdownMethod;
        private InputUpdateDelegate m_OnUpdate;
        private Action<InputUpdateType> m_OnBeforeUpdate;
        private Func<InputUpdateType, bool> m_OnShouldRunUpdate;
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

        public ScreenOrientation screenOrientation => Screen.orientation;

        public bool isInBatchMode => Application.isBatchMode;

        #if UNITY_EDITOR

        public bool isInPlayMode => EditorApplication.isPlaying;
        public bool isPaused => EditorApplication.isPaused;

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

        public void RegisterAnalyticsEvent(string name, int maxPerHour, int maxPropertiesPerEvent)
        {
            #if UNITY_ANALYTICS
            const string vendorKey = "unity.input";
            #if UNITY_EDITOR
            EditorAnalytics.RegisterEventWithLimit(name, maxPerHour, maxPropertiesPerEvent, vendorKey);
            #else
            Analytics.Analytics.RegisterEvent(name, maxPerHour, maxPropertiesPerEvent, vendorKey);
            #endif // UNITY_EDITOR
            #endif // UNITY_ANALYTICS
        }

        public void SendAnalyticsEvent(string name, object data)
        {
            #if UNITY_ANALYTICS
            #if UNITY_EDITOR
            EditorAnalytics.SendEventWithLimit(name, data);
            #else
            Analytics.Analytics.SendEvent(name, data);
            #endif // UNITY_EDITOR
            #endif // UNITY_ANALYTICS
        }
    }
}
