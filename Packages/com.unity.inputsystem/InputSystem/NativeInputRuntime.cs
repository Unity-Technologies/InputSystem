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
            if ((updateType & InputUpdateType.Dynamic) == InputUpdateType.Dynamic)
            {
                NativeInputSystem.Update(NativeInputUpdateType.Dynamic);
            }
            if ((updateType & InputUpdateType.Fixed) == InputUpdateType.Fixed)
            {
                NativeInputSystem.Update(NativeInputUpdateType.Fixed);
            }
            if ((updateType & InputUpdateType.BeforeRender) == InputUpdateType.BeforeRender)
            {
                NativeInputSystem.Update(NativeInputUpdateType.BeforeRender);
            }

            #if UNITY_EDITOR
            if ((updateType & InputUpdateType.Editor) == InputUpdateType.Editor)
            {
                NativeInputSystem.Update(NativeInputUpdateType.Editor);
            }
            #endif
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
                ////TODO: This is 2019.1-only right now but the native API change is in the process of getting backported
                ////      to 2018.3 (and hopefully 2018.2). What changed is that the native side now allows the managed side
                ////      to mutate the buffer and keep events around from update to update.

                if (value != null)
                ////TODO: temporary; remove when the native changes have landed in public 2019.1
                    #if false
                    //#if UNITY_2019_1_OR_NEWER
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
                    NativeInputSystem.onUpdate =
                        (updateType, eventCount, eventPtr) =>
                    {
                        var buffer = new InputEventBuffer((InputEvent*)eventPtr, eventCount);
                        value((InputUpdateType)updateType, ref buffer);
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

        public Action<int, string> onDeviceDiscovered
        {
            set { NativeInputSystem.onDeviceDiscovered = value; }
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
            set { NativeInputSystem.SetPollingFrequency(value); }
        }

        public double currentTime
        {
            get { return NativeInputSystem.currentTime; }
        }

        public double currentTimeOffsetToRealtimeSinceStartup
        {
            get { return NativeInputSystem.currentTimeOffsetToRealtimeSinceStartup; }
        }

        public double fixedUpdateIntervalInSeconds
        {
            get { return Time.fixedDeltaTime; }
        }

        public InputUpdateType updateMask
        {
            set { NativeInputSystem.SetUpdateMask((NativeInputUpdateType)value); }
        }

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

        public ScreenOrientation screenOrientation
        {
            get { return Screen.orientation; }
        }

        public Vector2 screenSize
        {
            get { return new Vector2(Screen.width, Screen.height); }
        }

        public int frameCount
        {
            get { return Time.frameCount; }
        }

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
