using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;

// need to be able to define hit area

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

        /// <summary>
        /// The input control that is created for this on-screen control.
        /// </summary>
        /// <remarks>
        /// This also provides access to the device.
        /// </remarks>
        [NonSerialized] internal InputControl m_Control;
        [SerializeField] internal string m_ControlPath;

        void OnEnable()
        {
            if (!string.IsNullOrEmpty(controlPath))
            {
                SetupInputControl();
            }
        }

        private void SetupInputControl()
        {
            var layout = InputControlPath.TryGetDeviceLayout(controlPath);
            var control = InputSystem.AddDevice(layout);
            m_Control = InputControlPath.TryFindControl(control, controlPath);
        }

        protected void SendStateEventToControl<TValue>(TValue value)
        {
            // NEED TO FIX THIS.   Only cast once.
            var control = m_Control as InputControl<TValue>;
            if (control == null)
            {
                throw new Exception(string.Format("The control path {0} yields a control of type {1} which is not an InputControl",
                        controlPath, m_Control.GetType().Name));
            }

            InputEventPtr eventPtr;
            var buffer = StateEvent.From(m_Control.device, out eventPtr);
            control.WriteValueInto(eventPtr, value);

            // NEED TO FIX THIS.
            //  eventPtr.time = InputRuntime.s_Runtime.currentTime;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();
        }
    }
}
