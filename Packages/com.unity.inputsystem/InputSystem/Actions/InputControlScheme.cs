using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: allow associating control schemes with platforms, too?

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Description of a control scheme.
    /// </summary>
    /// <remarks>
    /// Control schemes provide an additional layer on top of binding groups. While binding
    /// groups allow differentiating sets of bindings (e.g. a "Keyboard&amp;Mouse" group versus
    /// a "Gamepad" group),
    ///
    /// Control schemes fill this void by making the information explicit.
    /// </remarks>
    [Serializable]
    public struct InputControlScheme
    {
        /// <summary>
        /// Name of the control scheme.
        /// </summary>
        public string name
        {
            get { return m_Name; }
        }

        //problem: how do you do any subtractive operation? should we care?
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
        /// Binding group that is associated with the control scheme.
        /// </summary>
        /// <remarks>
        /// All bindings in this group as well as in groups inherited from base control schemes
        /// are considered to be part of the control scheme.
        /// </remarks>
        public string bindingGroup
        {
            get { return m_BindingGroup; }
            set { m_BindingGroup = value; }
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
            get { return new ReadOnlyArray<DeviceEntry>(m_Devices); }
        }

        public InputControlScheme(string name, string basedOn = null, IEnumerable<DeviceEntry> devices = null)
        {
            m_Name = name;
            m_BaseSchemeName = string.Empty;
            m_BindingGroup = name;
            m_BaseSchemeName = basedOn;
            m_Devices = null;

            if (devices != null)
            {
                m_Devices = devices.ToArray();
                if (m_Devices.Length == 0)
                    m_Devices = null;
            }
        }

        public string ToJson()
        {
            throw new NotImplementedException();
        }

        public static InputControlScheme FromJson(string json)
        {
            throw new NotImplementedException();
        }

        [SerializeField] internal string m_Name;
        [SerializeField] internal string m_BaseSchemeName;
        [SerializeField] internal string m_BindingGroup;
        [SerializeField] internal DeviceEntry[] m_Devices;

        [Serializable]
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
            public bool isOptional { get; set; }

            [SerializeField] private string m_DevicePath;
            [SerializeField] private bool m_IsOptional;

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(devicePath))
                {
                    if (isOptional)
                        return devicePath + "(Optional)";
                    return devicePath + "(Required)";
                }

                return base.ToString();
            }
        }

        /// <summary>
        /// JSON-serialized form of a control scheme.
        /// </summary>
        [Serializable]
        internal struct SchemeJson
        {
            public string name;
            public string basedOn;
            public string bindingGroup;
            public DeviceJson[] devices;

            [Serializable]
            public struct DeviceJson
            {
                public string devicePath;
                public bool isOptional;

                public DeviceEntry ToDeviceEntry()
                {
                    return new DeviceEntry
                    {
                        devicePath = devicePath,
                        isOptional = isOptional,
                    };
                }

                public static DeviceJson From(DeviceEntry entry)
                {
                    return new DeviceJson
                    {
                        devicePath = entry.devicePath,
                        isOptional = entry.isOptional,
                    };
                }
            }

            public InputControlScheme ToScheme()
            {
                DeviceEntry[] deviceEntries = null;
                if (devices != null && devices.Length > 0)
                {
                    var count = devices.Length;
                    deviceEntries = new DeviceEntry[count];
                    for (var i = 0; i < count; ++i)
                        deviceEntries[i] = devices[i].ToDeviceEntry();
                }

                return new InputControlScheme
                {
                    m_Name = string.IsNullOrEmpty(name) ? null : name,
                    m_BaseSchemeName = string.IsNullOrEmpty(basedOn) ? null : basedOn,
                    m_BindingGroup = string.IsNullOrEmpty(bindingGroup) ? null : bindingGroup,
                    m_Devices = deviceEntries,
                };
            }

            public static SchemeJson ToJson(InputControlScheme scheme)
            {
                DeviceJson[] devices = null;
                if (scheme.m_Devices != null && scheme.m_Devices.Length > 0)
                {
                    var count = scheme.m_Devices.Length;
                    devices = new DeviceJson[count];
                    for (var i = 0; i < count; ++i)
                        devices[i] = DeviceJson.From(scheme.m_Devices[i]);
                }

                return new SchemeJson
                {
                    name = scheme.m_Name,
                    basedOn = scheme.m_BaseSchemeName,
                    bindingGroup = scheme.m_BindingGroup,
                    devices = devices,
                };
            }

            public static SchemeJson[] ToJson(InputControlScheme[] schemes)
            {
                if (schemes == null || schemes.Length == 0)
                    return null;

                var count = schemes.Length;
                var result = new SchemeJson[count];

                for (var i = 0; i < count; ++i)
                    result[i] = ToJson(schemes[i]);

                return result;
            }

            public static InputControlScheme[] ToSchemes(SchemeJson[] schemes)
            {
                if (schemes == null || schemes.Length == 0)
                    return null;

                var count = schemes.Length;
                var result = new InputControlScheme[count];

                for (var i = 0; i < count; ++i)
                    result[i] = schemes[i].ToScheme();

                return result;
            }
        }
    }
}
