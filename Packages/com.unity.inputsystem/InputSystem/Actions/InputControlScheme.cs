using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Serialization;

////REVIEW: allow associating control schemes with platforms, too?

////REVIEW: move `baseScheme` entirely into JSON data only such that we resolve it during loading?
////        (and thus support it only input assets only)

////FIXME: doesn't show up in generated docs either; Doxygen is a fucking disaster

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A named set of zero or more device requirements along an associated binding group.
    /// </summary>
    /// <remarks>
    /// Control schemes provide an additional layer on top of binding groups. While binding
    /// groups allow differentiating sets of bindings (e.g. a "Keyboard&amp;Mouse" group versus
    /// a "Gamepad" group), control schemes impose a set of devices requirements that must be
    /// met in order for a specific set of bindings to be usable.
    /// </remarks>
    /// <seealso cref="InputActionAsset.controlSchemes"/>
    [Serializable]
    public struct InputControlScheme : IEquatable<InputControlScheme>
    {
        /// <summary>
        /// Name of the control scheme.
        /// </summary>
        /// <remarks>
        /// May be empty or null except if the control scheme is part of an <see cref="InputActionAsset"/>.
        /// </remarks>
        /// <seealso cref="InputActionAsset.AddControlScheme"/>
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
        /// of the control scheme to be considered satisfied. If, for example, one entry requires a "&lt;Gamepad&gt;" and
        /// another entry requires a "&lt;Gamepad&gt;", then at runtime two gamepads will be required even though a single
        /// one will match both requirements individually. However, if, for example, one entry requires "&lt;Gamepad&gt;/leftStick"
        /// and another requires "&lt;Gamepad&gt;, the same device can match both requirements as each one resolves to
        /// a different control.
        /// </remarks>
        public ReadOnlyArray<DeviceRequirement> deviceRequirements
        {
            get { return new ReadOnlyArray<DeviceRequirement>(m_DeviceRequirements); }
        }

        public InputControlScheme(string name, string basedOn = null, IEnumerable<DeviceRequirement> devices = null)
        {
            m_Name = name;
            m_BaseSchemeName = string.Empty;
            m_BindingGroup = name; // Defaults to name.
            m_BaseSchemeName = basedOn;
            m_DeviceRequirements = null;

            if (devices != null)
            {
                m_DeviceRequirements = devices.ToArray();
                if (m_DeviceRequirements.Length == 0)
                    m_DeviceRequirements = null;
            }
        }

        ////REVIEW: have mode where instead of matching only the first device that matches a requirement, we match as many
        ////        as we can get? (could be useful for single-player)
        /// <summary>
        /// Based on a list of devices, make a selection that matches the <see cref="deviceRequirements">requirements</see>
        /// imposed by the control scheme.
        /// </summary>
        /// <param name="devices">A list of devices to choose from.</param>
        /// <returns>A <see cref="MatchResult"/> structure containing the result of the pick. Note that this structure
        /// must be manually <see cref="MatchResult.Dispose">disposed</see> or unmanaged memory will be leaked.</returns>
        /// <remarks>
        /// Does not allocate managed memory.
        /// </remarks>
        public MatchResult PickDevicesFrom(InputControlList<InputDevice> devices)
        {
            // Empty device requirements match anything while not really picking anything.
            if (m_DeviceRequirements == null || m_DeviceRequirements.Length == 0)
            {
                return new MatchResult
                {
                    m_Result = MatchResult.Result.AllSatisfied,
                };
            }

            // Go through each requirement and match it.
            // NOTE: Even if `devices` is empty, we don't know yet whether we have a NoMatch.
            //       All our devices may be optional.
            var haveAllRequired = true;
            var haveAllOptional = true;
            var requirementCount = m_DeviceRequirements.Length;
            var controls = new InputControlList<InputControl>(Allocator.Persistent, requirementCount);
            try
            {
                var orChainIsSatisfied = false;
                var orChainHasRequiredDevices = false;
                for (var i = 0; i < requirementCount; ++i)
                {
                    var isOR = m_DeviceRequirements[i].isOR;
                    var isOptional = m_DeviceRequirements[i].isOptional;

                    // If this is an OR requirement and we already have a match in this OR chain,
                    // skip this requirement.
                    if (isOR && orChainIsSatisfied)
                    {
                        // Skill need to add an entry for this requirement.
                        controls.Add(null);
                        continue;
                    }

                    // Null and empty paths shouldn't make it into the list but make double
                    // sure here. Simply ignore entries that don't have a path.
                    var path = m_DeviceRequirements[i].controlPath;
                    if (string.IsNullOrEmpty(path))
                    {
                        controls.Add(null);
                        continue;
                    }

                    // Find the first matching control among the devices we have.
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
                        if (controls.Contains(matchedControl))
                            continue;

                        match = matchedControl;
                        break;
                    }

                    // Check requirements in AND and OR chains. We look ahead here to find out whether
                    // the next requirement is starting an OR chain. As the OR combines with the previous
                    // requirement in the list, this affects our current requirement.
                    var nextIsOR = i + 1 < requirementCount && m_DeviceRequirements[i + 1].isOR;
                    if (nextIsOR)
                    {
                        // Shouldn't get here if the chain is already satisfied. Should be handled
                        // at beginning of loop and we shouldn't even be looking at finding controls
                        // in that case.
                        Debug.Assert(!orChainIsSatisfied);

                        // It's an OR with the next requirement. Depends on the outcome of other matches whether
                        // we're good or not.

                        if (match != null)
                        {
                            // First match in this chain.
                            orChainIsSatisfied = true;
                        }
                        else
                        {
                            // Chain not satisfied yet.

                            if (!isOptional)
                                orChainHasRequiredDevices = true;
                        }
                    }
                    else if (isOR && i == requirementCount - 1)
                    {
                        // It's an OR at the very end of the requirements list. Terminate
                        // the OR chain.

                        if (match == null)
                        {
                            if (orChainHasRequiredDevices)
                                haveAllRequired = false;
                            else
                                haveAllOptional = false;
                        }
                    }
                    else
                    {
                        // It's an AND.

                        if (match == null)
                        {
                            if (isOptional)
                                haveAllOptional = false;
                            else
                                haveAllRequired = false;
                        }

                        // Terminate ongoing OR chain.
                        if (i > 0 && m_DeviceRequirements[i - 1].isOR)
                        {
                            if (!orChainIsSatisfied)
                            {
                                if (orChainHasRequiredDevices)
                                    haveAllRequired = false;
                                else
                                    haveAllOptional = false;
                            }
                            orChainIsSatisfied = false;
                        }
                    }

                    // Add match to list. Maybe null.
                    controls.Add(match);
                }

                // We should have matched each of our requirements.
                Debug.Assert(controls.Count == requirementCount);
            }
            catch (Exception)
            {
                controls.Dispose();
                throw;
            }

            return new MatchResult
            {
                m_Result = !haveAllRequired
                    ? MatchResult.Result.MissingRequired
                    : !haveAllOptional
                    ? MatchResult.Result.MissingOptional
                    : MatchResult.Result.AllSatisfied,
                m_Controls = controls,
                m_Requirements = m_DeviceRequirements,
            };
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
            if (m_DeviceRequirements == null || m_DeviceRequirements.Length == 0)
                return (other.m_DeviceRequirements == null || other.m_DeviceRequirements.Length == 0);
            if (other.m_DeviceRequirements == null || m_DeviceRequirements.Length != other.m_DeviceRequirements.Length)
                return false;

            var deviceCount = m_DeviceRequirements.Length;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = m_DeviceRequirements[i];
                var haveMatch = false;
                for (var n = 0; i < deviceCount; ++n)
                {
                    if (other.m_DeviceRequirements[n] == device)
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
                hashCode = (hashCode * 397) ^ (m_DeviceRequirements != null ? m_DeviceRequirements.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(m_Name))
                return base.ToString();

            if (m_DeviceRequirements == null)
                return m_Name;

            var builder = new StringBuilder();
            builder.Append(m_Name);
            builder.Append('(');

            var isFirst = true;
            foreach (var device in m_DeviceRequirements)
            {
                if (!isFirst)
                    builder.Append(',');

                builder.Append(device.controlPath);
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
        [SerializeField] internal DeviceRequirement[] m_DeviceRequirements;

        /// <summary>
        /// The result of matching a list of <see cref="InputDevice">devices</see> against a list of
        /// <see cref="DeviceRequirement">requirements</see> in an <see cref="InputControlScheme"/>.
        /// </summary>
        /// <seealso cref="InputControlScheme.PickDevicesFrom"/>
        public struct MatchResult : IEnumerable<MatchResult.Match>, IDisposable
        {
            /// <summary>
            /// Whether the device requirements got successfully matched.
            /// </summary>
            public bool isSuccessfulMatch
            {
                get { return m_Result != Result.MissingRequired; }
            }

            /// <summary>
            /// Whether there are missing required devices.
            /// </summary>
            /// <seealso cref="DeviceRequirement.isOptional"/>
            public bool hasMissingRequiredDevices
            {
                get { return m_Result == Result.MissingRequired; }
            }

            /// <summary>
            /// Whether there are missing optional devices.
            /// </summary>
            /// <seealso cref="DeviceRequirement.isOptional"/>
            public bool hasMissingOptionalDevices
            {
                get { return m_Result == Result.MissingOptional; }
            }

            /// <summary>
            /// The devices that got picked from the available devices.
            /// </summary>
            public InputControlList<InputDevice> devices
            {
                get
                {
                    // Lazily construct the device list. If we have missing required
                    // devices, though, always return an empty list. The user can still see
                    // the individual matches on each of the requirement entries but we
                    // consider the device picking itself failed.
                    if (m_Devices.Count == 0 && !hasMissingRequiredDevices)
                    {
                        var controlCount = m_Controls.Count;
                        if (controlCount != 0)
                        {
                            m_Devices.Capacity = controlCount;
                            for (var i = 0; i < controlCount; ++i)
                            {
                                var control = m_Controls[i];
                                if (control == null)
                                    continue;

                                var device = control.device;
                                if (m_Devices.Contains(device))
                                    continue;

                                m_Devices.Add(device);
                            }
                        }
                    }

                    return m_Devices;
                }
            }

            /// <summary>
            /// Enumerate the match for each individual <see cref="DeviceRequirement"/> in the control scheme.
            /// </summary>
            /// <returns>An enumerate going over each individual match.</returns>
            public IEnumerator<Match> GetEnumerator()
            {
                return new Enumerator
                {
                    m_Index = -1,
                    m_Requirements = m_Requirements,
                    m_Controls = m_Controls,
                };
            }

            /// <summary>
            /// Enumerate the match for each individual <see cref="DeviceRequirement"/> in the control scheme.
            /// </summary>
            /// <returns>An enumerate going over each individual match.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// Discard the list of devices.
            /// </summary>
            public void Dispose()
            {
                m_Controls.Dispose();
                m_Devices.Dispose();
            }

            internal Result m_Result;
            internal InputControlList<InputDevice> m_Devices;
            internal InputControlList<InputControl> m_Controls;
            internal DeviceRequirement[] m_Requirements;

            internal enum Result
            {
                AllSatisfied,
                MissingRequired,
                MissingOptional,
            }

            ////REVIEW: would be great to not have to repeatedly copy InputControlLists around

            /// <summary>
            /// A single matched <see cref="DeviceRequirement"/>.
            /// </summary>
            /// <remarks>
            /// Links the control that was matched with the respective device requirement.
            /// </remarks>
            public struct Match
            {
                /// <summary>
                /// The control that was match from the requirement's <see cref="DeviceRequirement.controlPath"/>
                /// </summary>
                /// <remarks>
                /// This is the same as <see cref="device"/> if the <see cref="DeviceRequirement.controlPath">control
                /// path</see> matches the device directly rather than matching a control on the device.
                ///
                /// Note that while a control path can match arbitrary many controls, only the first matched control
                /// will be returned here. To get all controls that were matched by a specific requirement, a
                /// manual query must be performed using <see cref="InputControlPath"/>.
                ///
                /// If the match failed, this will be null.
                /// </remarks>
                public InputControl control
                {
                    get { return m_Controls[m_RequirementIndex]; }
                }

                /// <summary>
                /// The device that got matched.
                /// </summary>
                /// <remarks>
                /// If a specific control on the device was matched, this will be <see cref="InputControl.device"/> or
                /// <see cref="control"/>. If a device was matched directly, this will be the same as <see cref="control"/>.
                /// </remarks>
                public InputDevice device
                {
                    get
                    {
                        var control = this.control;
                        return control == null ? null : control.device;
                    }
                }

                /// <summary>
                /// Index of the requirement in <see cref="InputControlScheme.deviceRequirements"/>.
                /// </summary>
                public int requirementIndex
                {
                    get { return m_RequirementIndex; }
                }

                /// <summary>
                /// The device requirement that got matched.
                /// </summary>
                public DeviceRequirement requirement
                {
                    get { return m_Requirements[m_RequirementIndex]; }
                }

                internal int m_RequirementIndex;
                internal DeviceRequirement[] m_Requirements;
                internal InputControlList<InputControl> m_Controls;
            }

            private struct Enumerator : IEnumerator<Match>
            {
                public bool MoveNext()
                {
                    ++m_Index;
                    return m_Requirements != null && m_Index < m_Requirements.Length;
                }

                public void Reset()
                {
                    m_Index = -1;
                }

                public Match Current
                {
                    get
                    {
                        if (m_Requirements == null || m_Index < 0 || m_Index >= m_Requirements.Length)
                            throw new InvalidOperationException("Enumerator is not valid");

                        return new Match
                        {
                            m_RequirementIndex = m_Index,
                            m_Requirements = m_Requirements,
                            m_Controls = m_Controls,
                        };
                    }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Dispose()
                {
                }

                internal int m_Index;
                internal DeviceRequirement[] m_Requirements;
                internal InputControlList<InputControl> m_Controls;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// Note that device requirements may require specific controls to be present rather than only requiring
        /// the presence of a certain type of device. For example, a requirement with a <see cref="controlPath"/>
        /// of "*/{PrimaryAction}" will be satisfied by any device that has a control marked as <see cref="CommonUsages.PrimaryAction"/>.
        ///
        /// Requirements are ordered in a list and can combine with their previous requirement in either <see cref="isAND">
        /// AND</see> or in <see cref="isOR">OR</see> fashion. The default is for requirements to combine with AND.
        ///
        /// Note that it is not possible to express nested constraints like <c>(a AND b) OR (c AND d)</c>. Also note that
        /// operator precedence is the opposite of C#, meaning that OR has *higher* precedence than AND. This means
        /// that <c>a OR b AND c OR d</c> reads as <c>(a OR b) AND (c OR d)</c> (in C# it would read as <c>a OR
        /// (b AND c) OR d</c>.
        ///
        /// More complex expressions can often be expressed differently. For example, <c>(a AND b) OR (c AND d)</c>
        /// can be expressed as <c>a OR c AND b OR d</c>.
        /// </remarks>
        [Serializable]
        public struct DeviceRequirement : IEquatable<DeviceRequirement>
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
            public string controlPath
            {
                get { return m_ControlPath; }
                set { m_ControlPath = value; }
            }

            /// <summary>
            /// If true, a device with the given <see cref="controlPath">device path</see> is employed by the
            /// control scheme if one is available. If none is available, the control scheme is still
            /// functional.
            /// </summary>
            public bool isOptional
            {
                get { return (m_Flags & Flags.Optional) != 0;}
                set
                {
                    if (value)
                        m_Flags |= Flags.Optional;
                    else
                        m_Flags &= ~Flags.Optional;
                }
            }

            /// <summary>
            /// Whether the requirement combines with the previous requirement (if any) as a boolean AND.
            /// </summary>
            /// <remarks>
            /// This is the default. For example, to require both a left hand and a right XR controller,
            /// the first requirement would be for "&lt;XRController&gt;{LeftHand}" and the second
            /// requirement would be for "&gt;XRController&gt;{RightHand}" and would return true for this
            /// property.
            /// </remarks>
            /// <seealso cref="isOR"/>
            public bool isAND
            {
                get { return !isOR; }
                set { isOR = !value; }
            }

            /// <summary>
            /// Whether the requirement combines with the previous requirement (if any) as a boolean OR.
            /// </summary>
            /// <remarks>
            /// This allows designing control schemes that flexibly work with combinations of devices such that
            /// if one specific device isn't present, another device can substitute for it.
            ///
            /// For example, to design a mouse+keyboard control scheme that can alternatively work with a pen
            /// instead of a mouse, the first requirement could be for "&lt;Keyboard&gt;", the second one
            /// could be for "&lt;Mouse&gt;" and the third one could be for "&lt;Pen&gt;" and return true
            /// for this property. Both the mouse and the pen would be marked as required (i.e. not <see cref="isOptional"/>)
            /// but the device requirements are satisfied even if either device is present.
            ///
            /// Note that if both a pen and a mouse are present at the same time, still only one device is
            /// picked. In this case, the mouse "wins" as it comes first in the list of requirements.
            /// </remarks>
            public bool isOR
            {
                get { return (m_Flags & Flags.Or) != 0;  }
                set
                {
                    if (value)
                        m_Flags |= Flags.Or;
                    else
                        m_Flags &= ~Flags.Or;
                }
            }

            [SerializeField] internal string m_ControlPath;
            [SerializeField] internal Flags m_Flags;

            [Flags]
            internal enum Flags
            {
                None = 0,
                Optional = 1 << 0,
                Or = 1 << 1,
            }

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(controlPath))
                {
                    if (isOptional)
                        return controlPath + "(Optional)";
                    return controlPath + "(Required)";
                }

                return base.ToString();
            }

            public bool Equals(DeviceRequirement other)
            {
                return string.Equals(m_ControlPath, other.m_ControlPath) && m_Flags == other.m_Flags &&
                    string.Equals(controlPath, other.controlPath) && isOptional == other.isOptional;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;

                return obj is DeviceRequirement && Equals((DeviceRequirement)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (m_ControlPath != null ? m_ControlPath.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ m_Flags.GetHashCode();
                    hashCode = (hashCode * 397) ^ (controlPath != null ? controlPath.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ isOptional.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator==(DeviceRequirement left, DeviceRequirement right)
            {
                return left.Equals(right);
            }

            public static bool operator!=(DeviceRequirement left, DeviceRequirement right)
            {
                return !left.Equals(right);
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
                public bool isOR;

                public DeviceRequirement ToDeviceEntry()
                {
                    return new DeviceRequirement
                    {
                        controlPath = devicePath,
                        isOptional = isOptional,
                        isOR = isOR,
                    };
                }

                public static DeviceJson From(DeviceRequirement requirement)
                {
                    return new DeviceJson
                    {
                        devicePath = requirement.controlPath,
                        isOptional = requirement.isOptional,
                        isOR = requirement.isOR,
                    };
                }
            }

            public InputControlScheme ToScheme()
            {
                DeviceRequirement[] deviceRequirements = null;
                if (devices != null && devices.Length > 0)
                {
                    var count = devices.Length;
                    deviceRequirements = new DeviceRequirement[count];
                    for (var i = 0; i < count; ++i)
                        deviceRequirements[i] = devices[i].ToDeviceEntry();
                }

                return new InputControlScheme
                {
                    m_Name = string.IsNullOrEmpty(name) ? null : name,
                    m_BaseSchemeName = string.IsNullOrEmpty(basedOn) ? null : basedOn,
                    m_BindingGroup = string.IsNullOrEmpty(bindingGroup) ? null : bindingGroup,
                    m_DeviceRequirements = deviceRequirements,
                };
            }

            public static SchemeJson ToJson(InputControlScheme scheme)
            {
                DeviceJson[] devices = null;
                if (scheme.m_DeviceRequirements != null && scheme.m_DeviceRequirements.Length > 0)
                {
                    var count = scheme.m_DeviceRequirements.Length;
                    devices = new DeviceJson[count];
                    for (var i = 0; i < count; ++i)
                        devices[i] = DeviceJson.From(scheme.m_DeviceRequirements[i]);
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
