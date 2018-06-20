using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    /// <summary>
    /// Base class for on-screen controls.
    /// </summary>
    /// <remarks>
    /// The set of on-screen controls together forms a device. A control layout
    /// is automatically generated from the set and a device using the layout is
    /// added to the system when the on-screen controls are enabled.
    ///
    /// The layout that the generated layout is based on is determined by the
    /// control paths chosen for each on-screen control. If, for example, an
    /// on-screen control chooses the 'a' key from the "Keyboard" layout as its
    /// path, a device layout is generated that is based on the "Keyboard" layout
    /// and the on-screen control becomes the 'a' key in that layout.
    ///
    /// If a GameObject has multiple on-screen controls that reference different
    /// types of device layouts (e.g. one control references 'buttonWest' on
    /// a gamepad and another references 'leftButton' on a mouse), then a device
    /// is created for each type referenced by the setup.
    /// </remarks>
    public abstract class OnScreenControl : MonoBehaviour
    {
        private struct OnScreenDeviceEventInfo
        {
            public InputEventPtr eventPtr;
            public NativeArray<byte> buffer;
            public InputDevice device;
        }

        private static InlinedArray<OnScreenDeviceEventInfo> s_DeviceEventInfoArray = new InlinedArray<OnScreenDeviceEventInfo>();

        // We will have N devices mapped to X number of OnScreenControls
        // Need to keep track and reference count how many controls are sharing
        // the same device, so that when the last control is ready to be destroyed
        // it can clean up the memory and remove the device from the input system.
        private static List<int> s_RegisteredOnScreenControls = new List<int>();

        public string controlPath
        {
            get { return m_ControlPath; }
            set
            {
                m_ControlPath = value;
                if (enabled)
                {
                    SetupInputControl();
                }
            }
        }

        [NonSerialized] internal InputControl m_Control;
        [SerializeField] internal string m_ControlPath;

        private int m_DeviceEventInfoIndex;
        private string m_Layout;

        private static int GetDeviceEventIndex(string layout)
        {
            for (int index = 0; index < s_DeviceEventInfoArray.length; index++)
            {
                if (s_DeviceEventInfoArray[index].device.layout == layout)
                    return index;
            }
            return -1;
        }

        private static int CreateOnScreenDevice(string layout)
        {
            var device = InputSystem.AddDevice(layout);
            InputEventPtr eventPtr;
            var buffer = StateEvent.From(device, out eventPtr, Allocator.Persistent);

            // Need to cache the buffer, device and InputEventPointer
            // so that muliple OnScreenControlInstances can share
            // the same OnScreenDevice and input memory;
            OnScreenDeviceEventInfo deviceEventInfo;
            deviceEventInfo.eventPtr = eventPtr;
            deviceEventInfo.buffer = buffer;
            deviceEventInfo.device = device;

            s_DeviceEventInfoArray.Append(deviceEventInfo);

            // Give the caller the index in the cache.
            return s_DeviceEventInfoArray.length - 1;
        }

        private static void RemoveOnScreenDevice(int id)
        {
            s_DeviceEventInfoArray[id].buffer.Dispose();
            InputSystem.RemoveDevice(s_DeviceEventInfoArray[id].device);
        }

        private static InputControl RegisterInputControl(string controlPath, out int id)
        {
            var layout = InputControlPath.TryGetDeviceLayout(controlPath);
            id = GetDeviceEventIndex(layout);

            // If we do not have a device created yet, create a new one
            // for OnScreenProcessing
            if (id < 0)
            {
                id = CreateOnScreenDevice(layout);
            }

            // If we couldn't create the device, need to error out
            if (id < 0)
            {
                throw new Exception(string.Format("Could nor create a device for the {0} control path",
                        controlPath));
            }

            var device = s_DeviceEventInfoArray[id].device;
            return InputControlPath.TryFindControl(device, controlPath);
        }

        private static void ProcessDeviceStateEventForValue<TValue>(int id, InputControl<TValue> control, TValue value)
        {
            var eventPtr = s_DeviceEventInfoArray[id].eventPtr;
            eventPtr.time = InputRuntime.s_Instance.currentTime;
            control.WriteValueInto(eventPtr, value);
            InputSystem.QueueEvent(eventPtr);
        }

        protected void SendValueToControl<TValue>(TValue value)
        {
            // NEED TO FIX THIS.   Only cast once.
            var control = m_Control as InputControl<TValue>;
            if (control == null)
            {
                throw new Exception(string.Format(
                        "The control path {0} yields a control of type {1} which is not an InputControl",
                        controlPath, m_Control.GetType().Name));
            }

            ProcessDeviceStateEventForValue(m_DeviceEventInfoIndex, control, value);
        }

        private void SetupInputControl()
        {
            m_Control = RegisterInputControl(controlPath, out m_DeviceEventInfoIndex);
            s_RegisteredOnScreenControls.Add(m_DeviceEventInfoIndex);
        }

        void OnEnable()
        {
            if (!string.IsNullOrEmpty(controlPath))
            {
                SetupInputControl();
            }
        }

        void OnDisable()
        {
            s_RegisteredOnScreenControls.Remove(m_DeviceEventInfoIndex);
            if (!s_RegisteredOnScreenControls.Contains(m_DeviceEventInfoIndex))
                RemoveOnScreenDevice(m_DeviceEventInfoIndex);
        }
    }
}
