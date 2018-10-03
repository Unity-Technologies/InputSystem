using System;
using Unity.Collections;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: should we make this ExecuteInEditMode?

////TODO: make this survive domain reloads

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

        public InputControl control
        {
            get { return m_Control; }
        }

        [SerializeField] internal string m_ControlPath;

        private InputControl m_Control;
        private OnScreenControl m_NextControlOnDevice;
        private InputEventPtr m_InputEventPtr;

        private void SetupInputControl()
        {
            Debug.Assert(m_Control == null);
            Debug.Assert(m_NextControlOnDevice == null);
            Debug.Assert(!m_InputEventPtr.valid);

            // Nothing to do if we don't have a control path.
            if (string.IsNullOrEmpty(m_ControlPath))
                return;

            // Determine what type of device to work with.
            var layoutName = InputControlPath.TryGetDeviceLayout(m_ControlPath);
            if (layoutName == null)
            {
                Debug.LogError(
                    string.Format(
                        "Cannot determine device layout to use based on control path '{0}' used in {1} component",
                        m_ControlPath, GetType().Name), this);
                return;
            }

            // Try to find existing on-screen device that matches.
            var internedLayoutName = new InternedString(layoutName);
            var deviceInfoIndex = -1;
            for (var i = 0; i < s_OnScreenDevices.length; ++i)
            {
                if (s_OnScreenDevices[i].device.m_Layout == internedLayoutName)
                {
                    deviceInfoIndex = i;
                    break;
                }
            }

            // If we don't have a matching one, create a new one.
            InputDevice device;
            if (deviceInfoIndex == -1)
            {
                // Try to create device.
                try
                {
                    device = InputSystem.AddDevice(layoutName);
                }
                catch (Exception exception)
                {
                    Debug.LogError(string.Format("Could not create device with layout '{0}' used in '{1}' component", layoutName,
                        GetType().Name));
                    Debug.LogException(exception);
                    return;
                }

                // Create event buffer.
                InputEventPtr eventPtr;
                var buffer = StateEvent.From(device, out eventPtr, Allocator.Persistent);

                // Add to list.
                deviceInfoIndex = s_OnScreenDevices.Append(new OnScreenDeviceInfo
                {
                    eventPtr = eventPtr,
                    buffer = buffer,
                    device = device,
                });
            }
            else
            {
                device = s_OnScreenDevices[deviceInfoIndex].device;
            }

            // Try to find control on device.
            m_Control = InputControlPath.TryFindControl(device, m_ControlPath);
            if (m_Control == null)
            {
                Debug.LogError(
                    string.Format(
                        "Cannot find control with path '{0}' on device of type '{1}' referenced by component '{2}'",
                        m_ControlPath, layoutName, GetType().Name), this);

                // Remove the device, if we just created one.
                if (s_OnScreenDevices[deviceInfoIndex].firstControl == null)
                {
                    s_OnScreenDevices[deviceInfoIndex].Destroy();
                    s_OnScreenDevices.RemoveAt(deviceInfoIndex);
                }

                return;
            }
            m_InputEventPtr = s_OnScreenDevices[deviceInfoIndex].eventPtr;

            // We have all we need. Permanently add us.
            s_OnScreenDevices[deviceInfoIndex] =
                s_OnScreenDevices[deviceInfoIndex].AddControl(this);
        }

        protected void SendValueToControl<TValue>(TValue value)
            where TValue : struct
        {
            if (m_Control == null)
                return;

            ////TODO: only cast once
            var control = m_Control as InputControl<TValue>;
            if (control == null)
            {
                throw new Exception(string.Format(
                    "The control path {0} yields a control of type {1} which is not an InputControl with value type {2}",
                    controlPath, m_Control.GetType().Name, typeof(TValue).Name));
            }

            m_InputEventPtr.internalTime = InputRuntime.s_Instance.currentTime;
            control.WriteValueInto(m_InputEventPtr, value);
            InputSystem.QueueEvent(m_InputEventPtr);
        }

        void OnEnable()
        {
            SetupInputControl();
        }

        void OnDisable()
        {
            if (m_Control != null)
            {
                var device = m_Control.device;
                for (var i = 0; i < s_OnScreenDevices.length; ++i)
                {
                    if (s_OnScreenDevices[i].device != device)
                        continue;

                    var deviceInfo = s_OnScreenDevices[i].RemoveControl(this);
                    if (deviceInfo.firstControl == null)
                    {
                        // We're the last on-screen control on this device. Remove the device.
                        s_OnScreenDevices[i].Destroy();
                        s_OnScreenDevices.RemoveAt(i);
                    }
                    else
                    {
                        s_OnScreenDevices[i] = deviceInfo;
                    }

                    m_Control = null;
                    m_InputEventPtr = new InputEventPtr();
                    Debug.Assert(m_NextControlOnDevice == null);

                    break;
                }
            }
        }

        private struct OnScreenDeviceInfo
        {
            public InputEventPtr eventPtr;
            public NativeArray<byte> buffer;
            public InputDevice device;
            public OnScreenControl firstControl;

            public OnScreenDeviceInfo AddControl(OnScreenControl control)
            {
                control.m_NextControlOnDevice = firstControl;
                firstControl = control;
                return this;
            }

            public OnScreenDeviceInfo RemoveControl(OnScreenControl control)
            {
                if (firstControl == control)
                    firstControl = control.m_NextControlOnDevice;
                else
                {
                    for (OnScreenControl current = firstControl.m_NextControlOnDevice, previous = firstControl;
                         current != null; previous = current, current = current.m_NextControlOnDevice)
                    {
                        if (current != control)
                            continue;

                        previous.m_NextControlOnDevice = current.m_NextControlOnDevice;
                        break;
                    }
                }

                control.m_NextControlOnDevice = null;
                return this;
            }

            public void Destroy()
            {
                if (buffer.IsCreated)
                    buffer.Dispose();
                if (device != null)
                    InputSystem.RemoveDevice(device);
                device = null;
                buffer = new NativeArray<byte>();
            }
        }

        private static InlinedArray<OnScreenDeviceInfo> s_OnScreenDevices;
    }
}
