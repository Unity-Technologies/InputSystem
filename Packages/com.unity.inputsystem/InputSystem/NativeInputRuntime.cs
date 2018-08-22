using System;
using UnityEngineInternal.Input;

#if UNITY_EDITOR
using UnityEditor;
#endif

// This should be the only file referencing the API at UnityEngineInternal.Input.

namespace UnityEngine.Experimental.Input.LowLevel
{
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

        public Action<InputUpdateType, int, IntPtr> onUpdate
        {
            set
            {
                // This is stupid but the enum prevents us from jacking the delegate in directly.
                // This means we get a double dispatch here :(
                if (value != null)
                    NativeInputSystem.onUpdate = (updateType, eventCount, eventPtr) =>
                        value((InputUpdateType)updateType, eventCount, eventPtr);
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

        public float pollingFrequency
        {
            set { NativeInputSystem.SetPollingFrequency(value); }
        }

        public double currentTime
        {
            get
            {
                return NativeInputSystem.currentTime;
            }
        }

        public double currentTimeOffsetToRealtimeSinceStartup
        {
            get
            {
                return NativeInputSystem.currentTimeOffsetToRealtimeSinceStartup;
            }
        }

        public InputUpdateType updateMask
        {
            set
            {
                NativeInputSystem.SetUpdateMask((NativeInputUpdateType)value);
            }
        }

        private Action m_ShutdownMethod;

        private void OnShutdown()
        {
            m_ShutdownMethod();
        }

        public ScreenOrientation screenOrientation
        {
            get
            {
                return Screen.orientation;
            }
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
            UnityEditor.EditorAnalytics.RegisterEventWithLimit(name, maxPerHour, maxPropertiesPerEvent, vendorKey);
            #else
            Analytics.Analytics.RegisterEvent(name, maxPerHour, maxPropertiesPerEvent, vendorKey);
            #endif
        }

        public void SendAnalyticsEvent(string name, object data)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorAnalytics.SendEventWithLimit(name, data);
            #else
            Analytics.Analytics.SendEvent(name, data);
            #endif
        }

        #endif // UNITY_ANALYTICS || UNITY_EDITOR
    }
}
