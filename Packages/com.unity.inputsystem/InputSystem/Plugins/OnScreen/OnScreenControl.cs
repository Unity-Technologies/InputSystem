using System;

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
        [SerializeField] private string m_ControlPath;

        /// <summary>
        /// The input control that is created for this on-screen control.
        /// </summary>
        /// <remarks>
        /// This also provides access to the device.
        /// </remarks>
        [NonSerialized] public InputControl m_Control;

        void OnEnable()
        {
        }

        public void SetControlPath(string controlPath)
        {
            m_ControlPath = controlPath;
            var layout = InputControlPath.TryGetDeviceLayout(m_ControlPath);
            var control = InputSystem.AddDevice(layout);
            m_Control = InputControlPath.TryFindControl(control, m_ControlPath);
        }
    }
}
