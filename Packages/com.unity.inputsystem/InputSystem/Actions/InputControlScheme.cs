using System;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A control scheme
    /// </summary>
    /// <remarks>
    /// Control schemes
    /// </remarks>
    public struct InputControlScheme
    {
        /// <summary>
        /// Name of the control scheme.
        /// </summary>
        public string name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// Name of control scheme that this scheme is based on.
        /// </summary>
        /// <remarks>
        /// When the control scheme is enabled, all bindings from the base control
        /// scheme will also be enabled. At the same time, bindings act as overrides on
        /// bindings coming through from the base scheme.
        /// </remarks>
        public string baseScheme
        {
            get { return m_BaseSchemeName; }
        }

        /// <summary>
        /// Devices necessary for this control scheme.
        /// </summary>
        /// <remarks>
        ///
        ///
        /// Note that there may be multiple devices
        /// </remarks>
        public ReadOnlyArray<DeviceEntry> devices
        {
            get { throw new NotImplementedException(); }
        }

        ////REVIEW: should these be multiple?
        /// <summary>
        /// Optional binding group that is associated with the control scheme.
        /// </summary>
        public string bindingGroup
        {
            get { throw new NotImplementedException(); }
        }

        private string m_Name;
        private string m_BaseSchemeName;

        public InputControlScheme(string name)
        {
            m_Name = name;
            m_BaseSchemeName = string.Empty;
        }

        public string ToJson()
        {
            throw new NotImplementedException();
        }

        public static InputControlScheme FromJson(string json)
        {
            throw new NotImplementedException();
        }

        public struct DeviceEntry
        {
            /// <summary>
            /// <see cref="InputControlPath">Control path</see> that is matched against a device to determine
            /// whether it qualifies for the control scheme.
            /// </summary>
            /// <remarks>
            /// </remarks>
            /// <example>
            /// <code>
            /// // A left-hand XR controller.
            /// "&lt;XRController&gt;{LeftHand}"
            ///
            /// // A gamepad.
            /// "&lt;Gamepad&gt;"
            /// </code>
            /// </example>
            public string devicePath { get; set; }

            /// <summary>
            /// If true, a device with the given <see cref="devicePath">device path</see> is employed by the
            /// control scheme if one is available. If none is available, the control scheme is still
            /// functional.
            /// </summary>
            public bool optional { get; set; }
        }
    }
}
