using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: allow associating control schemes with platforms, too?

////REVIEW: move `baseScheme` entirely into JSON data only such that we resolve it during loading?

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
    public struct InputControlScheme : IEquatable<InputControlScheme>
    {
        /// <summary>
        /// Name of the control scheme.
        /// </summary>
        public string name
        {
            get { return m_Name; }
        }

        //problem: how do you do any subtractive operation? should we care?
        //problem: this won't allow resolving things on just an InputControlScheme itself; needs context
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
        /// Devices used by the control scheme.
        /// </summary>
        /// <remarks>
        /// No two entries will be allowed to match the same control or device at runtime in order for the requirements
        /// of the control scheme to be considered <see cref="IsSatisfiedBy{TDeviceList}">satisfied</see>. If,
        /// for example, one entry requires a "&lt;Gamepad&gt;" and another entry requires a "&lt;Gamepad&gt;",
        /// then at runtime two gamepads will be required even though a single one will match both requirements
        /// individually.
        /// </remarks>
        public ReadOnlyArray<DeviceEntry> devices
        {
            get { return new ReadOnlyArray<DeviceEntry>(m_Devices); }
        }

        public InputControlScheme(string name, string basedOn = null, IEnumerable<DeviceEntry> devices = null)
        {
            m_Name = name;
            m_BaseSchemeName = string.Empty;
            m_BindingGroup = name; // Defaults to name.
            m_BaseSchemeName = basedOn;
            m_Devices = null;

            if (devices != null)
            {
                m_Devices = devices.ToArray();
                if (m_Devices.Length == 0)
                    m_Devices = null;
            }
        }

        /// <summary>
        /// Determine whether the given device matches any of the requirements in the control scheme.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <seealso cref="devices"/>
        public MatchResult Matches(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            // Empty device requirements matches any device.
            if (m_Devices == null || m_Devices.Length == 0)
                return MatchResult.NoSpecificRequirementsMatch;

            for (var i = 0; i < m_Devices.Length; ++i)
            {
                // Check if device path matches any control on the device. Works with both
                // matching requirements on child controls as well as matching requirements
                // just on the device itself.
                if (InputControlPath.TryFindControl(device, m_Devices[i].devicePath) != null)
                {
                    if (m_Devices[i].isOptional)
                        return MatchResult.OptionalMatch;
                    return MatchResult.RequiredMatch;
                }
            }

            return MatchResult.NoMatch;
        }

        /// <summary>
        /// Based on a list of devices, make a selection that matches the <see cref="devices">requirements</see>
        /// imposed by the control scheme. Remove any devices not used by the control scheme from the list.
        /// </summary>
        /// <param name="devices"></param>
        /// <returns></returns>
        public MatchResult PickMatchingDevices(ref InputControlList<InputDevice> devices)
        {
            // Empty device requirements matches anything.
            if (m_Devices == null || m_Devices.Length == 0)
            {
                devices.Clear();
                return MatchResult.NoSpecificRequirementsMatch;
            }

            // Go through each requirement and match it.
            // NOTE: Even if `devices` is empty, we don't know yet whether we have a NoMatch.
            //       All our devices may be optional.
            var result = MatchResult.RequiredMatch;
            var requirementCount = m_Devices.Length;
            using (var requirementMatches = new InputControlList<InputControl>(Allocator.Temp, requirementCount))
            {
                for (var i = 0; i < requirementCount; ++i)
                {
                    var path = m_Devices[i].devicePath;

                    InputControl match = null;
                    for (var n = 0; n < devices.Count; ++n)
                    {
                        var device = devices[n];

                        // See if we have a match.
                        var matchedControl = InputControlPath.TryFindControl(device, path);
                        if (matchedControl == null)
                            continue; // No.

                        // We have a match but if we've already match the same control through another requirement,
                        // we can't use the match.
                        if (requirementMatches.Contains(matchedControl))
                            continue;

                        match = matchedControl;
                        break;
                    }

                    var isOptional = m_Devices[i].isOptional;
                    if (match == null && !isOptional)
                        return MatchResult.NoMatch;

                    requirementMatches.Add(match);
                    if (isOptional)
                        result = MatchResult.OptionalMatch;
                }

                // We should have matched each of our requirements.
                Debug.Assert(requirementMatches.Count == requirementCount);

                // At this point, if we don't have any devices, we don't have a match.
                if (devices.Count == 0)
                    return MatchResult.NoMatch;

                // Whittle the list down to the devices that we picked.
                for (var i = 0; i < devices.Count; ++i)
                {
                    var device = devices[i];

                    var haveMatchedThisDevice = false;
                    for (var n = 0; n < requirementCount; ++n)
                    {
                        var matchedControl = requirementMatches[n];
                        var matchedDevice = matchedControl.device;

                        if (matchedDevice == device)
                        {
                            haveMatchedThisDevice = true;
                            break;
                        }
                    }

                    if (!haveMatchedThisDevice)
                    {
                        devices.RemoveAt(i);
                        --i;
                    }
                }
            }

            return result;
        }

        public string ToJson()
        {
            throw new NotImplementedException();
        }

        public static InputControlScheme FromJson(string json)
        {
            throw new NotImplementedException();
        }

        public bool Equals(InputControlScheme other)
        {
            if (!(string.Equals(m_Name, other.m_Name) &&
                  string.Equals(m_BaseSchemeName, other.m_BaseSchemeName) &&
                  string.Equals(m_BindingGroup, other.m_BindingGroup)))
                return false;

            // Compare device requirements.
            if (m_Devices == null || m_Devices.Length == 0)
                return (other.m_Devices == null || other.m_Devices.Length == 0);
            if (other.m_Devices == null || m_Devices.Length != other.m_Devices.Length)
                return false;

            var deviceCount = m_Devices.Length;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = m_Devices[i];
                var haveMatch = false;
                for (var n = 0; i < deviceCount; ++n)
                {
                    if (other.m_Devices[n] == device)
                    {
                        haveMatch = true;
                        break;
                    }
                }

                if (!haveMatch)
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is InputControlScheme && Equals((InputControlScheme)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (m_Name != null ? m_Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_BaseSchemeName != null ? m_BaseSchemeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_BindingGroup != null ? m_BindingGroup.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Devices != null ? m_Devices.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(m_Name))
                return base.ToString();

            if (m_Devices == null)
                return m_Name;

            var builder = new StringBuilder();
            builder.Append(m_Name);
            builder.Append('(');

            var isFirst = true;
            foreach (var device in m_Devices)
            {
                if (!isFirst)
                    builder.Append(',');

                builder.Append(device.devicePath);
                isFirst = false;
            }

            builder.Append(')');
            return builder.ToString();
        }

        public static bool operator==(InputControlScheme left, InputControlScheme right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(InputControlScheme left, InputControlScheme right)
        {
            return !left.Equals(right);
        }

        [SerializeField] internal string m_Name;
        [SerializeField] internal string m_BaseSchemeName;
        [SerializeField] internal string m_BindingGroup;
        [SerializeField] internal DeviceEntry[] m_Devices;

        public enum MatchResult
        {
            /// <summary>
            /// A <see cref="InputDevice">device</see> did not match any of a <see cref="InputControlScheme">
            /// control scheme's</see> device requirements.
            /// </summary>
            /// <seealso cref="InputControlScheme.devices"/>
            /// <seealso cref="InputControlScheme.Matches"/>
            NoMatch,

            RequiredMatch,
            OptionalMatch,

            /// <summary>
            /// The control scheme has no specific <see cref="devices">device requirements</see> and thus
            /// matched by virtue of matching anything.
            /// </summary>
            NoSpecificRequirementsMatch,
        }

        [Serializable]
        public struct DeviceEntry : IEquatable<DeviceEntry>
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
            public string devicePath
            {
                get { return m_DevicePath; }
                set { m_DevicePath = value; }
            }

            /// <summary>
            /// If true, a device with the given <see cref="devicePath">device path</see> is employed by the
            /// control scheme if one is available. If none is available, the control scheme is still
            /// functional.
            /// </summary>
            public bool isOptional
            {
                get { return m_IsOptional;}
                set { m_IsOptional = value; }
            }

            [SerializeField] private string m_DevicePath;
            [SerializeField] private bool m_IsOptional;////REVIEW: convert to flags field?

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

            public bool Equals(DeviceEntry other)
            {
                return string.Equals(m_DevicePath, other.m_DevicePath) && m_IsOptional == other.m_IsOptional &&
                    string.Equals(devicePath, other.devicePath) && isOptional == other.isOptional;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;

                return obj is DeviceEntry && Equals((DeviceEntry)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (m_DevicePath != null ? m_DevicePath.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ m_IsOptional.GetHashCode();
                    hashCode = (hashCode * 397) ^ (devicePath != null ? devicePath.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ isOptional.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator==(DeviceEntry left, DeviceEntry right)
            {
                return left.Equals(right);
            }

            public static bool operator!=(DeviceEntry left, DeviceEntry right)
            {
                return !left.Equals(right);
            }
        }

        public struct FilteredDeviceList<TSourceList> : IEnumerable<InputDevice>
            where TSourceList : IEnumerable<InputDevice>
        {
            public IEnumerator<InputDevice> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct DeviceEnumerator : IEnumerator<InputDevice>
        {
            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public InputDevice Current { get; private set; }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
                throw new NotImplementedException();
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
