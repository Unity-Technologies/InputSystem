using System;
using Unity.Collections;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////REVIEW: should we make this ExecuteInEditMode?

////TODO: give more control over when an OSC creates a new devices; going simply by name of layout only is inflexible

////TODO: make this survive domain reloads

////TODO: allow feeding into more than one control

namespace UnityEngine.InputSystem.OnScreen
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
    /// If a <see cref="GameObject"/> has multiple on-screen controls that reference different
    /// types of device layouts (e.g. one control references 'buttonWest' on
    /// a gamepad and another references 'leftButton' on a mouse), then a device
    /// is created for each type referenced by the setup.
    /// </remarks>
    public abstract class OnScreenControl : MonoBehaviour
    {
        public string controlPath
        {
            get => controlPathInternal;
            set
            {
                controlPathInternal = value;
                if (enabled)
                    SetupInputControl();
            }
        }

        /// <summary>
        /// The actual control that is fed input from the on-screen control.
        /// </summary>
        /// <remarks>
        /// This is only valid while the on-screen control is enabled. Otherwise, it is null. Also,
        /// if no <see cref="controlPath"/> has been set, this will remain null even if the component is enabled.
        /// </remarks>
        public InputControl control => m_Control;

        private InputControl m_Control;
        private OnScreenControl m_NextControlOnDevice;
        private InputEventPtr m_InputEventPtr;

        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// Moving the definition of how the control path is stored into subclasses allows them to
        /// apply their own <see cref="InputControlAttribute"/> attributes to them and thus set their
        /// own layout filters.
        /// </remarks>
        protected abstract string controlPathInternal { get; set; }

        private void SetupInputControl()
        {
            Debug.Assert(m_Control == null);
            Debug.Assert(m_NextControlOnDevice == null);
            Debug.Assert(!m_InputEventPtr.valid);

            // Nothing to do if we don't have a control path.
            var path = controlPathInternal;
            if (string.IsNullOrEmpty(path))
                return;

            // Determine what type of device to work with.
            var layoutName = InputControlPath.TryGetDeviceLayout(path);
            if (layoutName == null)
            {
                Debug.LogError(
                    $"Cannot determine device layout to use based on control path '{path}' used in {GetType().Name} component",
                    this);
                return;
            }

            // Try to find existing on-screen device that matches.
            var internedLayoutName = new InternedString(layoutName);
            var deviceInfoIndex = -1;
            for (var i = 0; i < s_OnScreenDevices.length; ++i)
            {
                ////FIXME: this does not take things such as different device usages into account
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
                    Debug.LogError(
                        $"Could not create device with layout '{layoutName}' used in '{GetType().Name}' component");
                    Debug.LogException(exception);
                    return;
                }

                // Create event buffer.
                var buffer = StateEvent.From(device, out var eventPtr, Allocator.Persistent);

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
            m_Control = InputControlPath.TryFindControl(device, path);
            if (m_Control == null)
            {
                Debug.LogError(
                    $"Cannot find control with path '{path}' on device of type '{layoutName}' referenced by component '{GetType().Name}'",
                    this);

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
            if (!(m_Control is InputControl<TValue> control))
                throw new ArgumentException(
                    $"The control path {controlPath} yields a control of type {m_Control.GetType().Name} which is not an InputControl with value type {typeof(TValue).Name}", nameof(value));

            ////FIXME: this gives us a one-frame lag
            m_InputEventPtr.internalTime = InputRuntime.s_Instance.currentTime;
            control.WriteValueIntoEvent(value, m_InputEventPtr);
            InputSystem.QueueEvent(m_InputEventPtr);
        }

        private void OnEnable()
        {
            SetupInputControl();
        }

        private void OnDisable()
        {
            if (m_Control == null)
                return;

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
