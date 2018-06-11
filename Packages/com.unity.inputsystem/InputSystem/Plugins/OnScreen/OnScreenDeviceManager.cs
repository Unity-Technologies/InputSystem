using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    public class OnScreenDeviceManager : MonoBehaviour
    {
        private struct OnScreenDeviceEventData
        {
            public InputEventPtr eventPtr;
            public NativeArray<byte> buffer;
        }

        Dictionary<InputDevice, OnScreenDeviceEventData> m_Devices = new Dictionary<InputDevice, OnScreenDeviceEventData>();

        static OnScreenDeviceManager s_Instance;

        public static OnScreenDeviceManager GetOnScreenDeviceManager()
        {
            if (s_Instance == null)
            {
                var gameObject = new GameObject("OnScreenDeviceManager");
                gameObject.hideFlags = HideFlags.HideInHierarchy;
                s_Instance = gameObject.AddComponent<OnScreenDeviceManager>();
                DontDestroyOnLoad(gameObject);
            }

            return s_Instance;
        }

        public InputEventPtr GetInputEventPtrForDevice(InputDevice device)
        {
            OnScreenDeviceEventData result;
            if (m_Devices.TryGetValue(device, out result))
            {
                return result.eventPtr;
            }

            result.buffer = StateEvent.From(device, out result.eventPtr);
            m_Devices[device] = result;

            return result.eventPtr;
        }

        void OnDestroy()
        {
            foreach (var device in m_Devices)
            {
                device.Value.buffer.Dispose();
            }
        }
    }
}
