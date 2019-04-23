using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngineInternal.Input;

#if UNITY_EDITOR
using UnityEditor;
#endif

// This should be the only file referencing the API at UnityEngineInternal.Input.

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Implements <see cref="IInputRuntime"/> based on <see cref="NativeInputSystem"/>.
    /// </summary>
    internal class NativeInputRuntime : IInputRuntime
    {
        public static NativeInputRuntime instance = new NativeInputRuntime();

        public int AllocateDeviceId()
        {
            return NativeInputSystem.AllocateDeviceId();
        }

        public void Update(InputUpdateType updateType)
        {
            NativeInputSystem.Update((NativeInputUpdateType)updateType);
        }

        public void QueueEvent(IntPtr ptr)
        {
            NativeInputSystem.QueueInputEvent(ptr);
        }

        public unsafe long DeviceCommand(int deviceId, InputDeviceCommand* commandPtr)
        {
            return NativeInputSystem.IOCTL(deviceId, commandPtr->type, new IntPtr(commandPtr->payloadPtr), commandPtr->payloadSizeInBytes);
        }

        public unsafe InputUpdateDelegate onUpdate
        {
            set
            {
                if (value != null)
                // This is 2019.2 (i.e. trunk at this point in time) only while we figure out how to get this
                // working properly.
                    #if UNITY_2019_2_OR_NEWER
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
                            Debug.LogError(string.Format(
                                "{0} during event processing of {1} update; resetting event buffer",
                                e.GetType().Name, updateType));
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
                    #else
                    // 2019.1 has the native API change but we need to fix the code in InputManager first
                    // before we can fully migrate to the new update code. For now, just manually reset
                    // the buffer here every time.
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
                        finally
                        {
                            // Need to account for the oddity of before render updates. This all goes
                            // away once we have the kinks worked out in InputManager.OnUpdate().
                            if (updateType != NativeInputUpdateType.BeforeRender)
                            {
                                eventBufferPtr->eventCount = 0;
                                eventBufferPtr->sizeInBytes = 0;
                            }
                        }
                    };
                    #endif
                else
                    NativeInputSystem.onUpdate = null;
            }
        }

        public Action<InputUpdateType> onBeforeUpdate
        {
            set
            {
                // This is stupid but the enum prevents us from jacking the delegate in directly.
                // This means we get a double dispatch here :(
                if (value != null)
                    NativeInputSystem.onBeforeUpdate = updateType => value((InputUpdateType)updateType);
                else
                    NativeInputSystem.onBeforeUpdate = null;
            }
        }

        public Func<InputUpdateType, bool> onShouldRunUpdate
        {
            set
            {
                // This is stupid but the enum prevents us from jacking the delegate in directly.
                // This means we get a double dispatch here :(
                if (value != null)
                    NativeInputSystem.onShouldRunUpdate = updateType => value((InputUpdateType)updateType);
                else
                    NativeInputSystem.onShouldRunUpdate = null;
            }
        }

        public Action<int, string> onDeviceDiscovered
        {
            set => NativeInputSystem.onDeviceDiscovered = value;
        }

        public Action onShutdown
        {
            set
            {
                if (value == null)
                {
                    #if UNITY_EDITOR
                    EditorApplication.quitting -= OnShutdown;
                    #else
                    Application.quitting -= OnShutdown;
                    #endif
                }
                else if (m_ShutdownMethod == null)
                {
                    #if UNITY_EDITOR
                    EditorApplication.quitting += OnShutdown;
                    #else
                    Application.quitting += OnShutdown;
                    #endif
                }

                m_ShutdownMethod = value;
            }
        }

        public Action<bool> onFocusChanged
        {
            set
            {
                if (value == null)
                #if UNITY_2019_1_OR_NEWER
                    Application.focusChanged -= OnFocusChanged;
                else if (m_FocusChangedMethod == null)
                    Application.focusChanged += OnFocusChanged;
                #endif
                    m_FocusChangedMethod = value;
            }
        }

        public float pollingFrequency
        {
            set => NativeInputSystem.SetPollingFrequency(value);
        }

        public double currentTime => NativeInputSystem.currentTime;

        public double currentTimeForFixedUpdate => Time.fixedUnscaledTime + currentTimeOffsetToRealtimeSinceStartup;

        public double currentTimeOffsetToRealtimeSinceStartup => NativeInputSystem.currentTimeOffsetToRealtimeSinceStartup;

        private Action m_ShutdownMethod;

        private void OnShutdown()
        {
            m_ShutdownMethod();
        }

        private Action<bool> m_FocusChangedMethod;

        private void OnFocusChanged(bool focus)
        {
            m_FocusChangedMethod(focus);
        }

        public ScreenOrientation screenOrientation => Screen.orientation;

        public Vector2 screenSize => new Vector2(Screen.width, Screen.height);

        public int frameCount => Time.frameCount;

        public bool isInBatchMode => Application.isBatchMode;

        #if UNITY_EDITOR

        public bool isInPlayMode => EditorApplication.isPlaying;
        public bool isPaused => EditorApplication.isPaused;

        public Action<PlayModeStateChange> onPlayModeChanged
        {
            set => EditorApplication.playModeStateChanged += value;
        }

        public Action onProjectChange
        {
            set => EditorApplication.projectChanged += value;
        }

        #endif // UNITY_EDITOR

        #if UNITY_ANALYTICS || UNITY_EDITOR

        public void RegisterAnalyticsEvent(string name, int maxPerHour, int maxPropertiesPerEvent)
        {
            const string vendorKey = "unity.input";
            #if UNITY_EDITOR
            EditorAnalytics.RegisterEventWithLimit(name, maxPerHour, maxPropertiesPerEvent, vendorKey);
            #else
            Analytics.Analytics.RegisterEvent(name, maxPerHour, maxPropertiesPerEvent, vendorKey);
            #endif
        }

        public void SendAnalyticsEvent(string name, object data)
        {
            #if UNITY_EDITOR
            EditorAnalytics.SendEventWithLimit(name, data);
            #else
            Analytics.Analytics.SendEvent(name, data);
            #endif
        }

        #endif // UNITY_ANALYTICS || UNITY_EDITOR
    }
}
