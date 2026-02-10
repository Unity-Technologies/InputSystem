using System;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////REVIEW: do we really need overridable processors and interactions?

// Downsides to the current approach:
// - Being able to address entire batches of controls through a single control is awesome. Especially
//   when combining it type-kind of queries (e.g. "<MyDevice>/<Button>"). However, it complicates things
//   in quite a few areas. There's quite a few bits in InputActionState that could be simplified if a
//   binding simply maps to a control.

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A mapping of controls to an action.
    /// </summary>
    /// <remarks>
    /// Each binding represents a value received from controls (see <see cref="InputControl"/>).
    /// There are two main types of bindings: "normal" bindings and "composite" bindings.
    ///
    /// Normal bindings directly bind to control(s) by means of <see cref="path"/> which is a "control path"
    /// (see <see cref="InputControlPath"/> for details about how to form paths). At runtime, the
    /// path of such a binding may match none, one, or multiple controls. Each control matched by the
    /// path will feed input into the binding.
    ///
    /// Composite bindings do not bind to controls themselves. Instead, they receive their input
    /// from their "part" bindings and then return a value representing a "composition" of those
    /// inputs. What composition specifically is performed depends on the type of the composite.
    /// <see cref="Composites.AxisComposite"/>, for example, will return a floating-point axis value
    /// computed from the state of two buttons.
    ///
    /// The action that is triggered by a binding is determined by its <see cref="action"/> property.
    /// The resolution to an <see cref="InputAction"/> depends on where the binding is used. For example,
    /// bindings that are part of <see cref="InputActionMap.bindings"/> will resolve action names to
    /// actions in the same <see cref="InputActionMap"/>.
    ///
    /// A binding can also be used as a form of search mask or filter. In this use, <see cref="path"/>,
    /// <see cref="action"/>, and <see cref="groups"/> become search criteria that are matched
    /// against other bindings. See <see cref="Matches(InputBinding)"/> for details. This use
    /// is employed in places such as <see cref="InputActionRebindingExtensions"/> as well as in
    /// binding masks on actions (<see cref="InputAction.bindingMask"/>), action maps (<see
    /// cref="InputActionMap.bindingMask"/>), and assets (<see cref="InputActionAsset.bindingMask"/>).
    /// </remarks>
    [Serializable]
    public struct InputBinding : IEquatable<InputBinding>
    {
        /// <summary>
        /// Character that is used to separate elements in places such as <see cref="groups"/>,
        /// <see cref="interactions"/>, and <see cref="processors"/>.
        /// </summary>
        /// Some strings on bindings represent lists of elements. An example is <see cref="groups"/>
        /// which may associate a binding with several binding groups, each one delimited by the
        /// separator.
        ///
        /// <remarks>
        /// <example>
        /// <code>
        /// // A binding that belongs to the "Keyboard&amp;Mouse" and "Gamepad" group.
        /// new InputBinding
        /// {
        ///     path = "*/{PrimaryAction}",
        ///     groups = "Keyboard&amp;Mouse;Gamepad"
        /// };
        /// </code>
        /// </example>
        /// </remarks>
        public const char Separator = ';';

        internal const string kSeparatorString = ";";

        /// <summary>
        /// Optional name for the binding.
        /// </summary>
        /// <value>Name of the binding.</value>
        /// <remarks>
        /// For bindings that are part of composites (see <see cref="isPartOfComposite"/>), this is
        /// the name of the field on the binding composite object that should be initialized with
        /// the control target of the binding.
        /// </remarks>
        public string name
        {
            get => m_Name;
            set => m_Name = value;
        }

        /// <summary>
        /// Unique ID of the binding.
        /// </summary>
        /// <value>Unique ID of the binding.</value>
        /// <remarks>
        /// This can be used, for example, when storing binding overrides in local user configurations.
        /// Using the binding ID, an override can remain associated with one specific binding.
        /// </remarks>
        public Guid id
        {
            get
            {
                ////REVIEW: this is inconsistent with InputActionMap and InputAction which generate IDs, if necessary
                if (string.IsNullOrEmpty(m_Id))
                    return default;
                return new Guid(m_Id);
            }
            set => m_Id = value.ToString();
        }

        /// <summary>
        /// Control path being bound to.
        /// </summary>
        /// <value>Path of control(s) to source input from.</value>
        /// <remarks>
        /// Bindings reference <see cref="InputControl"/>s using a regular expression-like
        /// language. See <see cref="InputControlPath"/> for details.
        ///
        /// If the binding is a composite (<see cref="isComposite"/>), the path is the composite
        /// string instead. For example, for a <see cref="Composites.Vector2Composite"/>, the
        /// path could be something like <c>"Vector2(normalize=false)"</c>.
        ///
        /// The path of a binding may be non-destructively override at runtime using <see cref="overridePath"/>
        /// which unlike this property is not serialized. <see cref="effectivePath"/> represents the
        /// final, effective path.
        /// </remarks>
        /// <example>
        /// <code>
        /// // A binding that references the left mouse button.
        /// new InputBinding { path = "&lt;Mouse&gt;/leftButton" }
        /// </code>
        /// </example>
        /// <seealso cref="overridePath"/>
        /// <seealso cref="InputControlPath"/>
        /// <seealso cref="InputControlPath.Parse"/>
        /// <seealso cref="InputControl.path"/>
        /// <seealso cref="InputSystem.FindControl"/>
        public string path
        {
            get => m_Path;
            set => m_Path = value;
        }

        /// <summary>
        /// If the binding is overridden, this is the overriding path.
        /// Otherwise it is <c>null</c>.
        /// </summary>
        /// <value>Path to override the <see cref="path"/> property with.</value>
        /// <remarks>
        /// Unlike the <see cref="path"/> property, the value of the override path is not serialized.
        /// If set, it will take precedence and determine the result of <see cref="effectivePath"/>.
        ///
        /// This property can be set to an empty string to disable the binding. During resolution,
        /// bindings with an empty <see cref="effectivePath"/> will get skipped.
        ///
        /// To set the override on an existing binding, use the methods supplied by <see cref="InputActionRebindingExtensions"/>
        /// such as <see cref="InputActionRebindingExtensions.ApplyBindingOverride(InputAction,string,string,string)"/>.
        ///
        /// <example>
        /// <code>
        /// // Override the binding to &lt;Gamepad&gt;/buttonSouth on
        /// // myAction with a binding to &lt;Gamepad&gt;/buttonNorth.
        /// myAction.ApplyBindingOverride(
        ///     new InputBinding
        ///     {
        ///         path = "&lt;Gamepad&gt;/buttonSouth",
        ///         overridePath = "&lt;Gamepad&gt;/buttonNorth"
        ///     });
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="path"/>
        /// <seealso cref="overrideInteractions"/>
        /// <seealso cref="overrideProcessors"/>
        /// <seealso cref="hasOverrides"/>
        /// <seealso cref="InputActionRebindingExtensions.SaveBindingOverridesAsJson(IInputActionCollection2)"/>
        /// <seealso cref="InputActionRebindingExtensions.LoadBindingOverridesFromJson(IInputActionCollection2,string,bool)"/>
        /// <seealso cref="InputActionRebindingExtensions.ApplyBindingOverride(InputAction,int,InputBinding)"/>
        public string overridePath
        {
            get => m_OverridePath;
            set => m_OverridePath = value;
        }

        /// <summary>
        /// Optional list of interactions and their parameters.
        /// </summary>
        /// <value>Interactions to put on the binding.</value>
        /// <remarks>
        /// Each element in the list is a name of an interaction (as registered with
        /// <see cref="InputSystem.RegisterInteraction{T}"/>) followed by an optional
        /// list of parameters.
        ///
        /// For example, <c>"slowTap(duration=1.2,pressPoint=0.123)"</c> is one element
        /// that puts a <see cref="Interactions.SlowTapInteraction"/> on the binding and
        /// sets <see cref="Interactions.SlowTapInteraction.duration"/> to 1.2 and
        /// <see cref="Interactions.SlowTapInteraction.pressPoint"/> to 0.123.
        ///
        /// Multiple interactions can be put on a binding by separating them with a comma.
        /// For example, <c>"tap,slowTap(duration=1.2)"</c> puts both a
        /// <see cref="Interactions.TapInteraction"/> and <see cref="Interactions.SlowTapInteraction"/>
        /// on the binding. See <see cref="IInputInteraction"/> for why the order matters.
        /// </remarks>
        /// <seealso cref="IInputInteraction"/>
        /// <seealso cref="overrideInteractions"/>
        /// <seealso cref="hasOverrides"/>
        /// <seealso cref="InputActionRebindingExtensions.SaveBindingOverridesAsJson(IInputActionCollection2)"/>
        /// <seealso cref="InputActionRebindingExtensions.LoadBindingOverridesFromJson(IInputActionCollection2,string,bool)"/>
        /// <seealso cref="InputActionRebindingExtensions.ApplyBindingOverride(InputAction,int,InputBinding)"/>
        public string interactions
        {
            get => m_Interactions;
            set => m_Interactions = value;
        }

        /// <summary>
        /// Interaction settings to override <see cref="interactions"/> with.
        /// </summary>
        /// <value>Override string for <see cref="interactions"/> or <c>null</c>.</value>
        /// <remarks>
        /// If this is not <c>null</c>, it replaces the value of <see cref="interactions"/>.
        /// </remarks>
        /// <seealso cref="effectiveInteractions"/>
        /// <seealso cref="interactions"/>
        /// <seealso cref="overridePath"/>
        /// <seealso cref="overrideProcessors"/>
        /// <seealso cref="hasOverrides"/>
        /// <seealso cref="InputActionRebindingExtensions.SaveBindingOverridesAsJson(IInputActionCollection2)"/>
        /// <seealso cref="InputActionRebindingExtensions.LoadBindingOverridesFromJson(IInputActionCollection2,string,bool)"/>
        /// <seealso cref="InputActionRebindingExtensions.ApplyBindingOverride(InputAction,int,InputBinding)"/>
        public string overrideInteractions
        {
            get => m_OverrideInteractions;
            set => m_OverrideInteractions = value;
        }

        /// <summary>
        /// Optional list of processors to apply to control values.
        /// </summary>
        /// <value>Value processors to apply to the binding.</value>
        /// <remarks>
        /// This string has the same format as <see cref="InputControlAttribute.processors"/>.
        /// </remarks>
        /// <seealso cref="InputProcessor{TValue}"/>
        /// <seealso cref="overrideProcessors"/>
        public string processors
        {
            get => m_Processors;
            set => m_Processors = value;
        }

        /// <summary>
        /// Processor settings to override <see cref="processors"/> with.
        /// </summary>
        /// <value>Override string for <see cref="processors"/> or <c>null</c>.</value>
        /// <remarks>
        /// If this is not <c>null</c>, it replaces the value of <see cref="processors"/>.
        /// </remarks>
        /// <seealso cref="effectiveProcessors"/>
        /// <seealso cref="processors"/>
        /// <seealso cref="overridePath"/>
        /// <seealso cref="overrideInteractions"/>
        /// <seealso cref="hasOverrides"/>
        public string overrideProcessors
        {
            get => m_OverrideProcessors;
            set => m_OverrideProcessors = value;
        }

        /// <summary>
        /// Optional list of binding groups that the binding belongs to.
        /// </summary>
        /// <value>List of binding groups or <c>null</c>.</value>
        /// <remarks>
        /// This is used, for example, to divide bindings into <see cref="InputControlScheme"/>s.
        /// Each control scheme is associated with a unique binding group through <see
        /// cref="InputControlScheme.bindingGroup"/>.
        ///
        /// A binding may be associated with multiple groups by listing each group name
        /// separate by a semicolon (<see cref="Separator"/>).
        ///
        /// <example>
        /// <code>
        /// new InputBinding
        /// {
        ///     path = "*/{PrimaryAction}",
        ///     // Associate the binding both with the "KeyboardMouse" and
        ///     // the "Gamepad" group.
        ///     groups = "KeyboardMouse;Gamepad",
        /// }
        /// </code>
        /// </example>
        ///
        /// Note that the system places no restriction on what binding groups are used
        /// for in practice. Their use by <see cref="InputControlScheme"/> is only one
        /// possible one, but which groups to apply and how to use them is ultimately
        /// up to you.
        /// </remarks>
        /// <seealso cref="InputControlScheme.bindingGroup"/>
        public string groups
        {
            get => m_Groups;
            set => m_Groups = value;
        }

        /// <summary>
        /// Name or ID of the action triggered by the binding.
        /// </summary>
        /// <remarks>
        /// This is null if the binding does not trigger an action.
        ///
        /// For InputBindings that are used as masks, this can be a "mapName/actionName" combination
        /// or "mapName/*" to match all actions in the given map.
        /// </remarks>
        /// <seealso cref="InputAction.name"/>
        /// <seealso cref="InputAction.id"/>
        public string action
        {
            get => m_Action;
            set => m_Action = value;
        }

        /// <summary>
        /// Whether the binding is a composite.
        /// </summary>
        /// <value>True if the binding is a composite.</value>
        /// <remarks>
        /// Composite bindings to not bind to controls to themselves but rather source their
        /// input from one or more "part binding" (see <see cref="isPartOfComposite"/>).
        ///
        /// See <see cref="InputBindingComposite{TValue}"/> for more details.
        /// </remarks>
        /// <seealso cref="InputBindingComposite{TValue}"/>
        public bool isComposite
        {
            get => (m_Flags & Flags.Composite) == Flags.Composite;
            set
            {
                if (value)
                    m_Flags |= Flags.Composite;
                else
                    m_Flags &= ~Flags.Composite;
            }
        }

        /// <summary>
        /// Whether the binding is a "part binding" of a composite.
        /// </summary>
        /// <value>True if the binding is part of a composite.</value>
        /// <remarks>
        /// The bindings that make up a composite are laid out sequentially in <see cref="InputActionMap.bindings"/>.
        /// First comes the composite itself which is flagged with <see cref="isComposite"/>. It mentions
        /// the composite and its parameters in its <see cref="path"/> property. After the composite itself come
        /// the part bindings. All subsequent bindings marked as <c>isPartOfComposite</c> will be associated
        /// with the composite.
        /// </remarks>
        /// <seealso cref="isComposite"/>
        /// <seealso cref="InputBindingComposite{TValue}"/>
        public bool isPartOfComposite
        {
            get => (m_Flags & Flags.PartOfComposite) == Flags.PartOfComposite;
            set
            {
                if (value)
                    m_Flags |= Flags.PartOfComposite;
                else
                    m_Flags &= ~Flags.PartOfComposite;
            }
        }

        /// <summary>
        /// True if any of the override properties, that is, <see cref="overridePath"/>, <see cref="overrideProcessors"/>,
        /// and/or <see cref="overrideInteractions"/>, are set (not <c>null</c>).
        /// </summary>
        public bool hasOverrides => overridePath != null || overrideProcessors != null || overrideInteractions != null;

        /// <summary>
        /// Initialize a new binding.
        /// </summary>
        /// <param name="path">Path for the binding.</param>
        /// <param name="action">Action to trigger from the binding.</param>
        /// <param name="groups">Semicolon-separated list of binding <see cref="InputBinding.groups"/> the binding is associated with.</param>
        /// <param name="processors">Comma-separated list of <see cref="InputBinding.processors"/> to apply to the binding.</param>
        /// <param name="interactions">Comma-separated list of <see cref="InputBinding.interactions"/> to apply to the
        /// binding.</param>
        /// <param name="name">Optional name for the binding.</param>
        public InputBinding(string path, string action = null, string groups = null, string processors = null,
                            string interactions = null, string name = null)
        {
            m_Path = path;
            m_Action = action;
            m_Groups = groups;
            m_Processors = processors;
            m_Interactions = interactions;
            m_Name = name;
            m_Id = default;
            m_Flags = default;
            m_OverridePath = default;
            m_OverrideInteractions = default;
            m_OverrideProcessors = default;
        }

        public string GetNameOfComposite()
        {
            if (!isComposite)
                return null;
            return NameAndParameters.Parse(effectivePath).name;
        }

        internal void GenerateId()
        {
            m_Id = Guid.NewGuid().ToString();
        }

        internal void RemoveOverrides()
        {
            m_OverridePath = null;
            m_OverrideInteractions = null;
            m_OverrideProcessors = null;
        }

        public static InputBinding MaskByGroup(string group)
        {
            return new InputBinding {groups = group};
        }

        public static InputBinding MaskByGroups(params string[] groups)
        {
            return new InputBinding {groups = string.Join(kSeparatorString, groups.Where(x => !string.IsNullOrEmpty(x)))};
        }

        [SerializeField] private string m_Name;
        [SerializeField] internal string m_Id;
        [Tooltip("Path of the control to bind to. Matched at runtime to controls from InputDevices present at the time.\n\nCan either be "
            + "graphically from the control picker dropdown UI or edited manually in text mode by clicking the 'T' button. Internally, both "
            + "methods result in control path strings that look like, for example, \"<Gamepad>/buttonSouth\".")]
        [SerializeField] private string m_Path;
        [SerializeField] private string m_Interactions;
        [SerializeField] private string m_Processors;
        [SerializeField] internal string m_Groups;
        [SerializeField] private string m_Action;
        [SerializeField] internal Flags m_Flags;

        [NonSerialized] private string m_OverridePath;
        [NonSerialized] private string m_OverrideInteractions;
        [NonSerialized] private string m_OverrideProcessors;

        /// <summary>
        /// This is the bindings path which is effectively being used.
        /// </summary>
        /// <remarks>
        /// This is either <see cref="overridePath"/> if that is set, or <see cref="path"/> otherwise.
        /// </remarks>
        public string effectivePath => overridePath ?? path;

        /// <summary>
        /// This is the interaction config which is effectively being used.
        /// </summary>
        /// <remarks>
        /// This is either <see cref="overrideInteractions"/> if that is set, or <see cref="interactions"/> otherwise.
        /// </remarks>
        public string effectiveInteractions => overrideInteractions ?? interactions;

        /// <summary>
        /// This is the processor config which is effectively being used.
        /// </summary>
        /// <remarks>
        /// This is either <see cref="overrideProcessors"/> if that is set, or <see cref="processors"/> otherwise.
        /// </remarks>
        public string effectiveProcessors => overrideProcessors ?? processors;

        internal bool isEmpty =>
            string.IsNullOrEmpty(effectivePath) && string.IsNullOrEmpty(action) &&
            string.IsNullOrEmpty(groups);

        /// <summary>
        /// Check whether the binding is equivalent to the given binding.
        /// </summary>
        /// <param name="other">Another binding.</param>
        /// <returns>True if the two bindings are equivalent.</returns>
        /// <remarks>
        /// Bindings are equivalent if their <see cref="effectivePath"/>, <see cref="effectiveInteractions"/>,
        /// and <see cref="effectiveProcessors"/>, plus their <see cref="action"/> and <see cref="groups"/>
        /// properties are the same. Note that the string comparisons ignore both case and culture.
        /// </remarks>
        public bool Equals(InputBinding other)
        {
            return string.Equals(effectivePath, other.effectivePath, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(effectiveInteractions, other.effectiveInteractions, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(effectiveProcessors, other.effectiveProcessors, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(groups, other.groups, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(action, other.action, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Compare the binding to the given object.
        /// </summary>
        /// <param name="obj">An object. May be <c>null</c>.</param>
        /// <returns>True if the given object is an <c>InputBinding</c> that equals this one.</returns>
        /// <seealso cref="Equals(InputBinding)"/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is InputBinding binding && Equals(binding);
        }

        /// <summary>
        /// Compare the two bindings for equality.
        /// </summary>
        /// <param name="left">The first binding.</param>
        /// <param name="right">The second binding.</param>
        /// <returns>True if the two bindings are equal.</returns>
        /// <seealso cref="Equals(InputBinding)"/>
        public static bool operator==(InputBinding left, InputBinding right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare the two bindings for inequality.
        /// </summary>
        /// <param name="left">The first binding.</param>
        /// <param name="right">The second binding.</param>
        /// <returns>True if the two bindings are not equal.</returns>
        /// <seealso cref="Equals(InputBinding)"/>
        public static bool operator!=(InputBinding left, InputBinding right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Compute a hash code for the binding.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (effectivePath != null ? effectivePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (effectiveInteractions != null ? effectiveInteractions.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (effectiveProcessors != null ? effectiveProcessors.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (groups != null ? groups.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (action != null ? action.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Return a string representation of the binding useful for debugging.
        /// </summary>
        /// <returns>A string representation of the binding.</returns>
        /// <example>
        /// <code>
        /// var binding = new InputBinding
        /// {
        ///     action = "fire",
        ///     path = "&lt;Gamepad&gt;/buttonSouth",
        ///     groups = "Gamepad"
        /// };
        ///
        /// // Returns "fire: &lt;Gamepad&gt;/buttonSouth [Gamepad]".
        /// binding.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            var builder = new StringBuilder();

            // Add action.
            if (!string.IsNullOrEmpty(action))
            {
                builder.Append(action);
                builder.Append(':');
            }

            // Add path.
            var path = effectivePath;
            if (!string.IsNullOrEmpty(path))
                builder.Append(path);

            // Add groups.
            if (!string.IsNullOrEmpty(groups))
            {
                builder.Append('[');
                builder.Append(groups);
                builder.Append(']');
            }

            return builder.ToString();
        }

        /// <summary>
        /// A set of flags to turn individual default behaviors of <see cref="InputBinding.ToDisplayString(DisplayStringOptions,InputControl)"/> off.
        /// </summary>
        [Flags]
        public enum DisplayStringOptions
        {
            /// <summary>
            /// Do not use short names of controls as set up by <see cref="InputControlAttribute.shortDisplayName"/>
            /// and the <c>"shortDisplayName"</c> property in JSON. This will, for example, not use LMB instead of "left Button"
            /// on <see cref="Mouse.leftButton"/>.
            /// </summary>
            DontUseShortDisplayNames = 1 << 0,

            /// <summary>
            /// By default device names are omitted from display strings. With this option, they are included instead.
            /// For example, <c>"A"</c> will be <c>"A [Gamepad]"</c> instead.
            /// </summary>
            DontOmitDevice = 1 << 1,

            /// <summary>
            /// By default, interactions on bindings are included in the resulting display string. For example, a binding to
            /// the gamepad's A button that has a "Hold" interaction on it, would come out as "Hold A". This can be suppressed
            /// with this option in which case the same setup would come out as just "A".
            /// </summary>
            DontIncludeInteractions = 1 << 2,

            /// <summary>
            /// By default, <see cref="effectivePath"/> is used for generating a display name. Using this option, the display
            /// string can be forced to <see cref="path"/> instead.
            /// </summary>
            IgnoreBindingOverrides = 1 << 3,
        }

        /// <summary>
        /// Turn the binding into a string suitable for display in a UI.
        /// </summary>
        /// <param name="options">Optional set of formatting options.</param>
        /// <param name="control">Optional control to which the binding has been resolved. If this is supplied,
        /// the resulting string can reflect things such as the current keyboard layout or hardware/platform-specific
        /// naming of controls (e.g. Xbox vs PS4 controllers as opposed to naming things generically based on the
        /// <see cref="Gamepad"/> layout).</param>
        /// <returns>A string representation of the binding suitable for display in a UI.</returns>
        /// <remarks>
        /// This method works only for bindings that are not composites. If the method is called on a binding
        /// that is a composite (<see cref="isComposite"/> is true), an empty string will be returned. To automatically
        /// handle composites, use <see cref="InputActionRebindingExtensions.GetBindingDisplayString(InputAction,DisplayStringOptions,string)"/>
        /// instead.
        ///
        /// <example>
        /// <code>
        /// var gamepadBinding = new InputBinding("&lt;Gamepad&gt;/buttonSouth");
        /// var mouseBinding = new InputBinding("&lt;Mouse&gt;/leftButton");
        /// var keyboardBinding = new InputBinding("&lt;Keyboard&gt;/a");
        ///
        /// // Prints "A" except on PS4 where it prints "Cross".
        /// Debug.Log(gamepadBinding.ToDisplayString());
        ///
        /// // Prints "LMB".
        /// Debug.Log(mouseBinding.ToDisplayString());
        ///
        /// // Print "Left Button".
        /// Debug.Log(mouseBinding.ToDisplayString(DisplayStringOptions.DontUseShortDisplayNames));
        ///
        /// // Prints the character associated with the "A" key on the current keyboard layout.
        /// Debug.Log(keyboardBinding, control: Keyboard.current);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControlPath.ToHumanReadableString(string,InputControlPath.HumanReadableStringOptions,InputControl)"/>
        /// <seealso cref="InputActionRebindingExtensions.GetBindingDisplayString(InputAction,int,InputBinding.DisplayStringOptions)"/>
        public string ToDisplayString(DisplayStringOptions options = default, InputControl control = default)
        {
            return ToDisplayString(out var _, out var _, options, control);
        }

        /// <summary>
        /// Turn the binding into a string suitable for display in a UI.
        /// </summary>
        /// <param name="options">Optional set of formatting options.</param>
        /// <param name="control">Optional control to which the binding has been resolved. If this is supplied,
        /// the resulting string can reflect things such as the current keyboard layout or hardware/platform-specific
        /// naming of controls (e.g. Xbox vs PS4 controllers as opposed to naming things generically based on the
        /// <see cref="Gamepad"/> layout).</param>
        /// <returns>A string representation of the binding suitable for display in a UI.</returns>
        /// <remarks>
        /// This method is the same as <see cref="ToDisplayString(DisplayStringOptions,InputControl)"/> except that it
        /// will also return the name of the device layout and path of the control, if applicable to the binding. This is
        /// useful when needing more context on the resulting display string, for example to decide on an icon to display
        /// instead of the textual display string.
        ///
        /// <example>
        /// <code>
        /// var displayString = new InputBinding("&lt;Gamepad&gt;/dpad/up")
        ///     .ToDisplayString(out deviceLayout, out controlPath);
        ///
        /// // Will print "D-Pad Up".
        /// Debug.Log(displayString);
        ///
        /// // Will print "Gamepad".
        /// Debug.Log(deviceLayout);
        ///
        /// // Will print "dpad/up".
        /// Debug.Log(controlPath);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControlPath.ToHumanReadableString(string,out string,out string,InputControlPath.HumanReadableStringOptions,InputControl)"/>
        /// <seealso cref="InputActionRebindingExtensions.GetBindingDisplayString(InputAction,int,out string,out string,InputBinding.DisplayStringOptions)"/>
        public string ToDisplayString(out string deviceLayoutName, out string controlPath, DisplayStringOptions options = default,
            InputControl control = default)
        {
            if (isComposite)
            {
                deviceLayoutName = null;
                controlPath = null;
                return string.Empty;
            }

            var readableStringOptions = default(InputControlPath.HumanReadableStringOptions);
            if ((options & DisplayStringOptions.DontOmitDevice) == 0)
                readableStringOptions |= InputControlPath.HumanReadableStringOptions.OmitDevice;
            if ((options & DisplayStringOptions.DontUseShortDisplayNames) == 0)
                readableStringOptions |= InputControlPath.HumanReadableStringOptions.UseShortNames;

            var pathToUse = (options & DisplayStringOptions.IgnoreBindingOverrides) != 0
                ? path
                : effectivePath;

            var result = InputControlPath.ToHumanReadableString(pathToUse, out deviceLayoutName, out controlPath, readableStringOptions, control);

            if (!string.IsNullOrEmpty(effectiveInteractions) && (options & DisplayStringOptions.DontIncludeInteractions) == 0)
            {
                var interactionString = string.Empty;
                var parsedInteractions = NameAndParameters.ParseMultiple(effectiveInteractions);

                foreach (var element in parsedInteractions)
                {
                    var interaction = element.name;
                    var interactionDisplayName = InputInteraction.GetDisplayName(interaction);

                    // An interaction can avoid being mentioned explicitly by just setting its display
                    // name to an empty string.
                    if (string.IsNullOrEmpty(interactionDisplayName))
                        continue;

                    ////TODO: this will need support for localization
                    if (!string.IsNullOrEmpty(interactionString))
                        interactionString = $"{interactionString} or {interactionDisplayName}";
                    else
                        interactionString = interactionDisplayName;
                }

                if (!string.IsNullOrEmpty(interactionString))
                    result = $"{interactionString} {result}";
            }

            return result;
        }

        internal bool TriggersAction(InputAction action)
        {
            // Match both name and ID on binding.
            return string.Compare(action.name, this.action, StringComparison.InvariantCultureIgnoreCase) == 0
                || this.action == action.m_Id;
        }

        ////TODO: also support matching by name (taking the binding tree into account so that components
        ////      of composites can be referenced through their parent)

        /// <summary>
        /// Check whether <paramref name="binding"/> matches the mask
        /// represented by the current binding.
        /// </summary>
        /// <param name="binding">An input binding.</param>
        /// <returns>True if <paramref name="binding"/> is matched by the mask represented
        /// by <c>this</c>.</returns>
        /// <remarks>
        /// In this method, the current binding acts as a "mask". When used this way, only
        /// three properties of the binding are taken into account: <see cref="path"/>,
        /// <see cref="groups"/>, and <see cref="action"/>.
        ///
        /// For each of these properties, the method checks whether they are set on the current
        /// binding and, if so, matches them against the respective property in <paramref name="binding"/>.
        ///
        /// The way this matching works is that the value of the property in the current binding is
        /// allowed to be a semicolon-separated list where each element specifies one possible value
        /// that will produce a match.
        ///
        /// Note that all comparisons are case-insensitive.
        ///
        /// <example>
        /// <code>
        /// // Create a couple bindings which we can test against.
        /// var keyboardBinding = new InputBinding
        /// {
        ///     path = "&lt;Keyboard&gt;/space",
        ///     groups = "Keyboard",
        ///     action = "Fire"
        /// };
        /// var gamepadBinding = new InputBinding
        /// {
        ///     path = "&lt;Gamepad&gt;/buttonSouth",
        ///     groups = "Gamepad",
        ///     action = "Jump"
        /// };
        /// var touchBinding = new InputBinding
        /// {
        ///     path = "&lt;Touchscreen&gt;/*/tap",
        ///     groups = "Touch",
        ///     action = "Jump"
        /// };
        ///
        /// // Example 1: Match any binding in the "Keyboard" or "Gamepad" group.
        /// var mask1 = new InputBinding
        /// {
        ///     // We put two elements in the list here and separate them with a semicolon.
        ///     groups = "Keyboard;Gamepad"
        /// };
        ///
        /// mask1.Matches(keyboardBinding); // True
        /// mask1.Matches(gamepadBinding); // True
        /// mask1.Matches(touchBinding); // False
        ///
        /// // Example 2: Match any binding to the "Jump" or the "Roll" action
        /// //            (the latter we don't actually have a binding for)
        /// var mask2 = new InputBinding
        /// {
        ///     action = "Jump;Roll"
        /// };
        ///
        /// mask2.Matches(keyboardBinding); // False
        /// mask2.Matches(gamepadBinding); // True
        /// mask2.Matches(touchBinding); // True
        ///
        /// // Example: Match any binding to the space or enter key in the
        /// //          "Keyboard" group.
        /// var mask3 = new InputBinding
        /// {
        ///     path = "&lt;Keyboard&gt;/space;&lt;Keyboard&gt;/enter",
        ///     groups = "Keyboard"
        /// };
        ///
        /// mask3.Matches(keyboardBinding); // True
        /// mask3.Matches(gamepadBinding); // False
        /// mask3.Matches(touchBinding); // False
        /// </code>
        /// </example>
        /// </remarks>
        public bool Matches(InputBinding binding)
        {
            return Matches(ref binding);
        }

        // Internally we pass by reference to not unnecessarily copy the struct.
        internal bool Matches(ref InputBinding binding, MatchOptions options = default)
        {
            // Match name.
            if (!string.IsNullOrEmpty(name))
            {
                if (string.IsNullOrEmpty(binding.name)
                    || !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(name, binding.name, Separator))
                    return false;
            }

            // Match path.
            if (path != null)
            {
                ////REVIEW: should this use binding.effectivePath?
                ////REVIEW: should this support matching by prefix only? should this use control path matching instead of just string comparisons?
                ////TODO: handle things like ignoring leading '/'
                if (binding.path == null
                    || !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(path, binding.path, Separator))
                    return false;
            }

            // Match action.
            if (action != null)
            {
                ////TODO: handle "map/action" format
                ////TODO: handle "map/*" format
                ////REVIEW: this will not be able to handle cases where one binding references an action by ID and the other by name but both do mean the same action
                if (binding.action == null
                    || !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(action, binding.action, Separator))
                    return false;
            }

            // Match groups.
            if (groups != null)
            {
                var haveGroupsOnBinding = !string.IsNullOrEmpty(binding.groups);
                if (!haveGroupsOnBinding && (options & MatchOptions.EmptyGroupMatchesAny) == 0)
                    return false;

                if (haveGroupsOnBinding
                    && !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(groups, binding.groups, Separator))
                    return false;
            }

            // Match ID.
            if (!string.IsNullOrEmpty(m_Id))
            {
                if (binding.id != id)
                    return false;
            }

            return true;
        }

        [Flags]
        internal enum MatchOptions
        {
            EmptyGroupMatchesAny = 1 << 0,
        }

        [Flags]
        internal enum Flags
        {
            None = 0,
            Composite = 1 << 2,
            PartOfComposite = 1 << 3,
        }
    }
}
