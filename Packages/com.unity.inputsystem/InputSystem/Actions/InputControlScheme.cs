using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

////TODO: introduce the concept of a "variation"
////      - a variation is just a variant of a control scheme, not a full control scheme by itself
////      - an individual variation can be toggled on and off independently
////      - while a control is is active, all its variations that are toggled on are also active
////      - assignment to variations works the same as assignment to control schemes
////  use case: left/right stick toggles, left/right bumper toggles, etc

////TODO: introduce concept of precedence where one control scheme will be preferred over another that is also a match
////      (might be its enough to represent this simply through ordering by giving the user control over the ordering through the UI)

////REVIEW: allow associating control schemes with platforms, too?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A named set of zero or more device requirements along with an associated binding group.
    /// </summary>
    /// <remarks>
    /// Control schemes provide an additional layer on top of binding groups. While binding
    /// groups allow differentiating sets of bindings (e.g. a "Keyboard&amp;Mouse" group versus
    /// a "Gamepad" group), control schemes impose a set of devices requirements that must be
    /// met in order for a specific set of bindings to be usable.
    ///
    /// Note that control schemes can only be defined at the <see cref="InputActionAsset"/> level.
    /// </remarks>
    /// <seealso cref="InputActionAsset.controlSchemes"/>
    /// <seealso cref="InputActionSetupExtensions.AddControlScheme(InputActionAsset,string)"/>
    [Serializable]
    public struct InputControlScheme : IEquatable<InputControlScheme>
    {
        /// <summary>
        /// Name of the control scheme. Not <c>null</c> or empty except if InputControlScheme
        /// instance is invalid (i.e. default-initialized).
        /// </summary>
        /// <value>Name of the scheme.</value>
        /// <remarks>
        /// May be empty or null except if the control scheme is part of an <see cref="InputActionAsset"/>.
        /// </remarks>
        /// <seealso cref="InputActionSetupExtensions.AddControlScheme(InputActionAsset,string)"/>
        public string name => m_Name;

        /// <summary>
        /// Binding group that is associated with the control scheme. Not <c>null</c> or empty
        /// except if InputControlScheme is invalid (i.e. default-initialized).
        /// </summary>
        /// <value>Binding group for the scheme.</value>
        /// <remarks>
        /// All bindings in this group are considered to be part of the control scheme.
        /// </remarks>
        /// <seealso cref="InputBinding.groups"/>
        public string bindingGroup
        {
            get => m_BindingGroup;
            set => m_BindingGroup = value;
        }

        /// <summary>
        /// Devices used by the control scheme.
        /// </summary>
        /// <value>Device requirements of the scheme.</value>
        /// <remarks>
        /// No two entries will be allowed to match the same control or device at runtime in order for the requirements
        /// of the control scheme to be considered satisfied. If, for example, one entry requires a "&lt;Gamepad&gt;" and
        /// another entry requires a "&lt;Gamepad&gt;", then at runtime two gamepads will be required even though a single
        /// one will match both requirements individually. However, if, for example, one entry requires "&lt;Gamepad&gt;/leftStick"
        /// and another requires "&lt;Gamepad&gt;, the same device can match both requirements as each one resolves to
        /// a different control.
        ///
        /// It it allowed to define control schemes without device requirements, i.e. for which this
        /// property will be an empty array. Note, however, that features such as automatic control scheme
        /// switching in <see cref="PlayerInput"/> will not work with such control schemes.
        /// </remarks>
        public ReadOnlyArray<DeviceRequirement> deviceRequirements =>
            new ReadOnlyArray<DeviceRequirement>(m_DeviceRequirements);

        /// <summary>
        /// Initialize the control scheme with the given name, device requirements,
        /// and binding group.
        /// </summary>
        /// <param name="name">Name to use for the scheme. Required.</param>
        /// <param name="devices">List of device requirements.</param>
        /// <param name="bindingGroup">Name to use for the binding group (see <see cref="InputBinding.groups"/>)
        /// associated with the control scheme. If this is <c>null</c> or empty, <paramref name="name"/> is
        /// used instead (with <see cref="InputBinding.Separator"/> characters stripped from the name).</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c> or empty.</exception>
        public InputControlScheme(string name, IEnumerable<DeviceRequirement> devices = null, string bindingGroup = null)
            : this()
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            SetNameAndBindingGroup(name, bindingGroup);

            m_DeviceRequirements = null;
            if (devices != null)
            {
                m_DeviceRequirements = devices.ToArray();
                if (m_DeviceRequirements.Length == 0)
                    m_DeviceRequirements = null;
            }
        }

        #if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
        internal InputControlScheme(SerializedProperty sp)
        {
            var requirements = new List<DeviceRequirement>();
            var deviceRequirementsArray = sp.FindPropertyRelative(nameof(m_DeviceRequirements));
            if (deviceRequirementsArray == null)
                throw new ArgumentException("The serialized property does not contain an InputControlScheme object.");

            foreach (SerializedProperty deviceRequirement in deviceRequirementsArray)
            {
                requirements.Add(new DeviceRequirement
                {
                    controlPath = deviceRequirement.FindPropertyRelative(nameof(DeviceRequirement.m_ControlPath)).stringValue,
                    m_Flags = (DeviceRequirement.Flags)deviceRequirement.FindPropertyRelative(nameof(DeviceRequirement.m_Flags)).enumValueFlag
                });
            }

            m_Name = sp.FindPropertyRelative(nameof(m_Name)).stringValue;
            m_DeviceRequirements = requirements.ToArray();
            m_BindingGroup = sp.FindPropertyRelative(nameof(m_BindingGroup)).stringValue;
        }

        #endif

        internal void SetNameAndBindingGroup(string name, string bindingGroup = null)
        {
            m_Name = name;
            if (!string.IsNullOrEmpty(bindingGroup))
                m_BindingGroup = bindingGroup;
            else
                m_BindingGroup = name.Contains(InputBinding.Separator)
                    ? name.Replace(InputBinding.kSeparatorString, "")
                    : name;
        }

        /// <summary>
        /// Given a list of devices and a list of control schemes, find the most suitable control
        /// scheme to use with the devices.
        /// </summary>
        /// <param name="devices">A list of devices. If the list is empty, only schemes with
        /// empty <see cref="deviceRequirements"/> lists will get matched.</param>
        /// <param name="schemes">A list of control schemes.</param>
        /// <param name="mustIncludeDevice">If not <c>null</c>, a successful match has to include the given device.</param>
        /// <param name="allowUnsuccesfulMatch">If true, then allow returning a match that has unsatisfied requirements but still
        /// matched at least some requirement. If there are several unsuccessful matches, the returned scheme is still the highest
        /// scoring one among those.</param>
        /// <typeparam name="TDevices">Collection type to use for the list of devices.</typeparam>
        /// <typeparam name="TSchemes">Collection type to use for the list of schemes.</typeparam>
        /// <returns>The control scheme that best matched the given devices or <c>null</c> if no
        /// scheme was found suitable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="devices"/> is <c>null</c> -or-
        /// <paramref name="schemes"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Any successful match (see <see cref="MatchResult.isSuccessfulMatch"/>) will be considered.
        /// The one that matches the most amount of devices (see <see cref="MatchResult.devices"/>)
        /// will be returned. If more than one schemes matches equally well, the first one encountered
        /// in the list is returned.
        ///
        /// Note that schemes are not required to match all devices available in the list. The result
        /// will simply be the scheme that matched the most devices of what was devices. Use <see
        /// cref="PickDevicesFrom{TDevices}"/> to find the devices that a control scheme selects.
        ///
        /// This method is parameterized over <typeparamref name="TDevices"/> and <typeparamref name="TSchemes"/>
        /// to allow avoiding GC heap allocations from boxing of structs such as <see cref="ReadOnlyArray{TValue}"/>.
        ///
        /// <example>
        /// <code>
        /// // Create an .inputactions asset.
        /// var asset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
        ///
        /// // Add some control schemes to the asset.
        /// asset.AddControlScheme("KeyboardMouse")
        ///     .WithRequiredDevice&lt;Keyboard&gt;()
        ///     .WithRequiredDevice&lt;Mouse&gt;());
        /// asset.AddControlScheme("Gamepad")
        ///     .WithRequiredDevice&lt;Gamepad&gt;());
        /// asset.AddControlScheme("DualGamepad")
        ///     .WithRequiredDevice&lt;Gamepad&gt;())
        ///     .WithOptionalGamepad&lt;Gamepad&gt;());
        ///
        /// // Add some devices that we can test with.
        /// var keyboard = InputSystem.AddDevice&lt;Keyboard&gt;();
        /// var mouse = InputSystem.AddDevice&lt;Mouse&gt;();
        /// var gamepad1 = InputSystem.AddDevice&lt;Gamepad&gt;();
        /// var gamepad2 = InputSystem.AddDevice&lt;Gamepad&gt;();
        ///
        /// // Matching with just a keyboard won't match any scheme.
        /// InputControlScheme.FindControlSchemeForDevices(
        ///     new InputDevice[] { keyboard }, asset.controlSchemes);
        ///
        /// // Matching with a keyboard and mouse with match the "KeyboardMouse" scheme.
        /// InputControlScheme.FindControlSchemeForDevices(
        ///     new InputDevice[] { keyboard, mouse }, asset.controlSchemes);
        ///
        /// // Matching with a single gamepad will match the "Gamepad" scheme.
        /// // Note that since the second gamepad is optional in "DualGamepad" could
        /// // match the same set of devices but it doesn't match any better than
        /// // "Gamepad" and that one comes first in the list.
        /// InputControlScheme.FindControlSchemeForDevices(
        ///     new InputDevice[] { gamepad1 }, asset.controlSchemes);
        ///
        /// // Matching with two gamepads will match the "DualGamepad" scheme.
        /// // Note that "Gamepad" will match this device list as well. If "DualGamepad"
        /// // didn't exist, "Gamepad" would be the result here. However, "DualGamepad"
        /// // matches the list better than "Gamepad" so that's what gets returned here.
        /// InputControlScheme.FindControlSchemeForDevices(
        ///     new InputDevice[] { gamepad1, gamepad2 }, asset.controlSchemes);
        /// </code>
        /// </example>
        /// </remarks>
        public static InputControlScheme? FindControlSchemeForDevices<TDevices, TSchemes>(TDevices devices, TSchemes schemes, InputDevice mustIncludeDevice = null, bool allowUnsuccesfulMatch = false)
            where TDevices : IReadOnlyList<InputDevice>
            where TSchemes : IEnumerable<InputControlScheme>
        {
            if (devices == null)
                throw new ArgumentNullException(nameof(devices));
            if (schemes == null)
                throw new ArgumentNullException(nameof(schemes));

            if (!FindControlSchemeForDevices(devices, schemes, out var controlScheme, out var matchResult, mustIncludeDevice, allowUnsuccesfulMatch))
                return null;

            matchResult.Dispose();
            return controlScheme;
        }

        public static bool FindControlSchemeForDevices<TDevices, TSchemes>(TDevices devices, TSchemes schemes,
            out InputControlScheme controlScheme, out MatchResult matchResult, InputDevice mustIncludeDevice = null, bool allowUnsuccessfulMatch = false)
            where TDevices : IReadOnlyList<InputDevice>
            where TSchemes : IEnumerable<InputControlScheme>
        {
            if (devices == null)
                throw new ArgumentNullException(nameof(devices));
            if (schemes == null)
                throw new ArgumentNullException(nameof(schemes));

            MatchResult? bestResult = null;
            InputControlScheme? bestScheme = null;

            foreach (var scheme in schemes)
            {
                var result = scheme.PickDevicesFrom(devices, favorDevice: mustIncludeDevice);

                // Ignore if scheme doesn't fit devices.
                if (!result.isSuccessfulMatch && (!allowUnsuccessfulMatch || result.score <= 0))
                {
                    result.Dispose();
                    continue;
                }

                // Ignore if we have a device we specifically want to be part of the result and
                // the current match doesn't have it.
                if (mustIncludeDevice != null && !result.devices.Contains(mustIncludeDevice))
                {
                    result.Dispose();
                    continue;
                }

                // Ignore if it does fit but we already have a better fit.
                if (bestResult != null && bestResult.Value.score >= result.score)
                {
                    result.Dispose();
                    continue;
                }

                bestResult?.Dispose();

                bestResult = result;
                bestScheme = scheme;
            }

            matchResult = bestResult ?? default;
            controlScheme = bestScheme ?? default;

            return bestResult.HasValue;
        }

        ////FIXME: docs are wrong now
        /// <summary>
        /// Return the first control schemes from the given list that supports the given
        /// device (see <see cref="SupportsDevice"/>).
        /// </summary>
        /// <param name="device">An input device.</param>
        /// <param name="schemes">A list of control schemes. Can be empty.</param>
        /// <typeparam name="TSchemes">Collection type to use for the list of schemes.</typeparam>
        /// <returns>The first schemes from <paramref name="schemes"/> that supports <paramref name="device"/>
        /// or <c>null</c> if none of the schemes is usable with the device.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c> -or-
        /// <paramref name="schemes"/> is <c>null</c>.</exception>
        public static InputControlScheme? FindControlSchemeForDevice<TSchemes>(InputDevice device, TSchemes schemes)
            where TSchemes : IEnumerable<InputControlScheme>
        {
            if (schemes == null)
                throw new ArgumentNullException(nameof(schemes));
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            return FindControlSchemeForDevices(new OneOrMore<InputDevice, ReadOnlyArray<InputDevice>>(device), schemes);
        }

        /// <summary>
        /// Whether the control scheme has a requirement in <see cref="deviceRequirements"/> that
        /// targets the given device.
        /// </summary>
        /// <param name="device">An input device.</param>
        /// <returns>True if the control scheme has a device requirement matching the device.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Note that both optional (see <see cref="DeviceRequirement.isOptional"/>) and non-optional
        /// device requirements are taken into account.
        ///
        /// </remarks>
        public bool SupportsDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            ////REVIEW: does this need to take AND and OR into account?
            for (var i = 0; i < m_DeviceRequirements.Length; ++i)
            {
                var control = InputControlPath.TryFindControl(device, m_DeviceRequirements[i].controlPath);
                if (control != null)
                    return true;
            }

            return false;
        }

        ////REVIEW: have mode where instead of matching only the first device that matches a requirement, we match as many
        ////        as we can get? (could be useful for single-player)
        /// <summary>
        /// Based on a list of devices, make a selection that matches the <see cref="deviceRequirements">requirements</see>
        /// imposed by the control scheme.
        /// </summary>
        /// <param name="devices">A list of devices to choose from.</param>
        /// <param name="favorDevice">If not null, the device will be favored over other devices in <paramref name="devices"/>.
        /// Note that the device must be present in the list also.</param>
        /// <returns>A <see cref="MatchResult"/> structure containing the result of the pick. Note that this structure
        /// must be manually <see cref="MatchResult.Dispose">disposed</see> or unmanaged memory will be leaked.</returns>
        /// <remarks>
        /// Does not allocate managed memory.
        /// </remarks>
        public MatchResult PickDevicesFrom<TDevices>(TDevices devices, InputDevice favorDevice = null)
            where TDevices : IReadOnlyList<InputDevice>
        {
            // Empty device requirements match anything while not really picking anything.
            if (m_DeviceRequirements == null || m_DeviceRequirements.Length == 0)
            {
                return new MatchResult
                {
                    m_Result = MatchResult.Result.AllSatisfied,
                    // Prevent zero score on successful match but make less than one which would
                    // result from having a single requirement.
                    m_Score = 0.5f,
                };
            }

            // Go through each requirement and match it.
            // NOTE: Even if `devices` is empty, we don't know yet whether we have a NoMatch.
            //       All our devices may be optional.
            var haveAllRequired = true;
            var haveAllOptional = true;
            var requirementCount = m_DeviceRequirements.Length;
            var score = 0f;
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
                        score += 1;
                        controls.Add(null);
                        continue;
                    }

                    // Find the first matching control among the devices we have.
                    InputControl match = null;
                    for (var n = 0; n < devices.Count; ++n)
                    {
                        var device = devices[n];

                        // If we should favor a device, we swap it in at index #0 regardless
                        // of where in the list the device occurs (it MUST, however, occur in the list).
                        if (favorDevice != null)
                        {
                            if (n == 0)
                                device = favorDevice;
                            else if (device == favorDevice)
                                device = devices[0];
                        }

                        // See if we have a match.
                        var matchedControl = InputControlPath.TryFindControl(device, path);
                        if (matchedControl == null)
                            continue; // No.

                        // We have a match but if we've already matched the same control through another requirement,
                        // we can't use the match.
                        if (controls.Contains(matchedControl))
                            continue;

                        match = matchedControl;

                        // Compute score for match.
                        var deviceLayoutOfControlPath = new InternedString(InputControlPath.TryGetDeviceLayout(path));
                        if (deviceLayoutOfControlPath.IsEmpty())
                        {
                            // Generic match adds 1 to score.
                            score += 1;
                        }
                        else
                        {
                            var deviceLayoutOfControl = matchedControl.device.m_Layout;
                            if (InputControlLayout.s_Layouts.ComputeDistanceInInheritanceHierarchy(deviceLayoutOfControlPath,
                                deviceLayoutOfControl, out var distance))
                            {
                                score += 1 + 1f / (Math.Abs(distance) + 1);
                            }
                            else
                            {
                                // Shouldn't really get here as for the control to be a match for the path, the device layouts
                                // would be expected to be related to each other. But just add 1 for a generic match and go on.
                                score += 1;
                            }
                        }

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
                m_Score = score,
            };
        }

        public bool Equals(InputControlScheme other)
        {
            if (!(string.Equals(m_Name, other.m_Name, StringComparison.InvariantCultureIgnoreCase) &&
                  string.Equals(m_BindingGroup, other.m_BindingGroup, StringComparison.InvariantCultureIgnoreCase)))
                return false;

            // Compare device requirements.
            if (m_DeviceRequirements == null || m_DeviceRequirements.Length == 0)
                return other.m_DeviceRequirements == null || other.m_DeviceRequirements.Length == 0;
            if (other.m_DeviceRequirements == null || m_DeviceRequirements.Length != other.m_DeviceRequirements.Length)
                return false;

            var deviceCount = m_DeviceRequirements.Length;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = m_DeviceRequirements[i];
                var haveMatch = false;
                for (var n = 0; n < deviceCount; ++n)
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
        [SerializeField] internal string m_BindingGroup;
        [SerializeField] internal DeviceRequirement[] m_DeviceRequirements;

        /// <summary>
        /// The result of matching a list of <see cref="InputDevice">devices</see> against a list of
        /// <see cref="DeviceRequirement">requirements</see> in an <see cref="InputControlScheme"/>.
        /// </summary>
        /// <remarks>
        /// This struct uses <see cref="InputControlList{TControl}"/> which allocates unmanaged memory
        /// and thus must be disposed in order to not leak unmanaged heap memory.
        /// </remarks>
        /// <seealso cref="InputControlScheme.PickDevicesFrom{TDevices}"/>
        public struct MatchResult : IEnumerable<MatchResult.Match>, IDisposable
        {
            /// <summary>
            /// Overall, relative measure for how well the control scheme matches.
            /// </summary>
            /// <value>Scoring value for the control scheme match.</value>
            /// <remarks>
            /// Two control schemes may, for example, both support gamepads but one may be tailored to a specific
            /// gamepad whereas the other one is a generic gamepad control scheme. To differentiate the two, we need
            /// to know not only that a control schemes but how well it matches relative to other schemes. This is
            /// what the score value is used for.
            ///
            /// Scores are computed primarily based on layouts referenced from device requirements. To start with, each
            /// matching device requirement (whether optional or mandatory) will add 1 to the score. This the base
            /// score of a match. Then, for each requirement a delta is computed from the device layout referenced by
            /// the requirement to the device layout used by the matching control. For example, if the requirement is
            /// <c>"&lt;Gamepad&gt;</c> and the matching control uses the <see cref="DualShock.DualShock4GamepadHID"/>
            /// layout, the delta is 2 as the latter layout is derived from <see cref="Gamepad"/> via the intermediate
            /// <see cref="DualShock.DualShockGamepad"/> layout, i.e. two steps in the inheritance hierarchy. The
            /// <em>inverse</em> of the delta plus one, i.e. <c>1/(delta+1)</c> is then added to the score. This means
            /// that an exact match will add an additional 1 to the score and less exact matches will add progressively
            /// smaller values to the score (proportional to the distance of the actual layout to the one used in the
            /// requirement).
            ///
            /// What this leads to is that, for example, a control scheme with a <c>"&lt;Gamepad&gt;"</c> requirement
            /// will match a <see cref="DualShock.DualShock4GamepadHID"/> with a <em>lower</em> score than a control
            /// scheme with a <c>"&lt;DualShockGamepad&gt;"</c> requirement as the <see cref="Gamepad"/> layout is
            /// further removed (i.e. smaller inverse delta) from <see cref="DualShock.DualShock4GamepadHID"/> than
            /// <see cref="DualShock.DualShockGamepad"/>.
            /// </remarks>
            public float score => m_Score;

            /// <summary>
            /// Whether the device requirements got successfully matched.
            /// </summary>
            /// <value>True if the scheme's device requirements were satisfied.</value>
            public bool isSuccessfulMatch => m_Result != Result.MissingRequired;

            /// <summary>
            /// Whether there are missing required devices.
            /// </summary>
            /// <value>True if there are missing, non-optional devices.</value>
            /// <seealso cref="DeviceRequirement.isOptional"/>
            public bool hasMissingRequiredDevices => m_Result == Result.MissingRequired;

            /// <summary>
            /// Whether there are missing optional devices. This does not prevent
            /// a successful match.
            /// </summary>
            /// <value>True if there are missing optional devices.</value>
            /// <seealso cref="DeviceRequirement.isOptional"/>
            public bool hasMissingOptionalDevices => m_Result == Result.MissingOptional;

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
                                    continue; // Duplicate match of same device.

                                m_Devices.Add(device);
                            }
                        }
                    }

                    return m_Devices;
                }
            }

            public Match this[int index]
            {
                get
                {
                    if (index < 0 || m_Requirements == null || index >= m_Requirements.Length)
                        throw new ArgumentOutOfRangeException("index");
                    return new Match
                    {
                        m_RequirementIndex = index,
                        m_Requirements = m_Requirements,
                        m_Controls = m_Controls,
                    };
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
            internal float m_Score;
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
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Conflicts with UnityEngine.Networking.Match, which is deprecated and will go away.")]
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
                public InputControl control => m_Controls[m_RequirementIndex];

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
                        return control?.device;
                    }
                }

                /// <summary>
                /// Index of the requirement in <see cref="InputControlScheme.deviceRequirements"/>.
                /// </summary>
                public int requirementIndex => m_RequirementIndex;

                /// <summary>
                /// The device requirement that got matched.
                /// </summary>
                public DeviceRequirement requirement => m_Requirements[m_RequirementIndex];

                public bool isOptional => requirement.isOptional;

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

                object IEnumerator.Current => Current;

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
                get => m_ControlPath;
                set => m_ControlPath = value;
            }

            /// <summary>
            /// If true, a device with the given <see cref="controlPath">device path</see> is employed by the
            /// control scheme if one is available. If none is available, the control scheme is still
            /// functional.
            /// </summary>
            public bool isOptional
            {
                get => (m_Flags & Flags.Optional) != 0;
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
                get => !isOR;
                set => isOR = !value;
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
                get => (m_Flags & Flags.Or) != 0;
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
                        return controlPath + " (Optional)";
                    return controlPath + " (Required)";
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
