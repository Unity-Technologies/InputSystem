using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.UI;

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
            public OnScreenControl firstControl;
        }

        private static InlinedArray<OnScreenDeviceEventInfo> s_DeviceEventInfoArray = new InlinedArray<OnScreenDeviceEventInfo>();

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

        private OnScreenControl m_NextControlOnDevice;
        private string m_Layout;
        private InputEventPtr m_InputEventPtr;

        private static int GetDeviceEventIndex(string layout)
        {
            for (int index = 0; index < s_DeviceEventInfoArray.length; index++)
            {
                if (s_DeviceEventInfoArray[index].device.layout == layout)
                    return index;
            }

            return -1;
        }

        private static InputDevice CreateOnScreenDevice(string layout, OnScreenControl onScreenControl)
        {
            var device = InputSystem.AddDevice(layout);
            InputEventPtr eventPtr;
            var buffer = StateEvent.From(device, out eventPtr, Allocator.Persistent);

            OnScreenDeviceEventInfo deviceEventInfo;
            deviceEventInfo.eventPtr = eventPtr;
            deviceEventInfo.buffer = buffer;
            deviceEventInfo.device = device;
            deviceEventInfo.firstControl = onScreenControl;

            s_DeviceEventInfoArray.Append(deviceEventInfo);

            return device;
        }

        private static void RemoveOnScreenDevice(int id)
        {
            s_DeviceEventInfoArray[id].buffer.Dispose();
            InputSystem.RemoveDevice(s_DeviceEventInfoArray[id].device);
        }

        private void ProcessDeviceStateEventForValue<TValue>(InputControl<TValue> control, TValue value)
        {
            m_InputEventPtr.time = InputRuntime.s_Instance.currentTime;
            control.WriteValueInto(m_InputEventPtr, value);
            InputSystem.QueueEvent(m_InputEventPtr);
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

            ProcessDeviceStateEventForValue(control, value);
        }

        private InputControl RegisterInputControl(string controlPath)
        {
            var layout = InputControlPath.TryGetDeviceLayout(controlPath);

            if (layout == null)
            {
                throw new Exception(string.Format("Could not parse a device layout for the {0} control path",
                        controlPath));
            }

            m_Layout = layout;

            // Check if we already have a a device created for this type of OnScreenControl
            int deviceIndex = GetDeviceEventIndex(layout);

            // If we do not have a device created yet, create a new one
            if (deviceIndex < 0)
            {
                var device = CreateOnScreenDevice(layout, this);
                if (device == null)
                {
                    throw new Exception(string.Format("Could not create a device for the {0} control path",
                            controlPath));
                }

                return InputControlPath.TryFindControl(device, controlPath);
            }
            else
            {
                m_NextControlOnDevice = s_DeviceEventInfoArray[deviceIndex].firstControl.m_NextControlOnDevice;
                var temp = s_DeviceEventInfoArray[deviceIndex].firstControl;
                temp.m_NextControlOnDevice = this;

                return InputControlPath.TryFindControl(s_DeviceEventInfoArray[deviceIndex].device, controlPath);
            }
        }

        private void SetupInputControl()
        {
            m_Control = RegisterInputControl(controlPath);
            m_InputEventPtr = s_DeviceEventInfoArray[GetDeviceEventIndex(m_Layout)].eventPtr;
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
            int deviceIndex = GetDeviceEventIndex(m_Layout);
            if (deviceIndex != -1)
            {
                if (s_DeviceEventInfoArray[deviceIndex].firstControl.m_NextControlOnDevice == null)
                {
                    RemoveOnScreenDevice(deviceIndex);
                    s_DeviceEventInfoArray.RemoveAt(deviceIndex);
                }
                else
                {
                    // Not going to search entire list to match exact object.  Just remove head of list
                    // Unit we get to no other devices left.
                    s_DeviceEventInfoArray[deviceIndex].firstControl.m_NextControlOnDevice =
                        s_DeviceEventInfoArray[deviceIndex].firstControl.m_NextControlOnDevice.m_NextControlOnDevice;
                }
            }
        }
    }
}
