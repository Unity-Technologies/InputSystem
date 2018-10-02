using System;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: should bindings have unique IDs, too? maybe instead of "name"?

////REVIEW: do we really need overridable processors and interactions?

////REVIEW: do we really need "name"?

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A mapping of control input to an action.
    /// </summary>
    /// <remarks>
    /// A single binding can match arbitrary many controls through its path and then
    /// map their input to a single action.
    ///
    /// A binding can also be used as a override specification. In that scenario, <see cref="path"/>,
    /// <see cref="action"/>, and <see cref="groups"/> become search criteria that can be used to
    /// find existing bindings, and <see cref="overridePath"/> becomes the path to override existing
    /// binding paths with.
    ///
    /// Finally, a binding can be used as a form of specifying a mask that matching bindings must
    /// comply to. For example, a binding that has only <see cref="groups"/> set to "Gamepad" and all
    /// other fields set to default can be used to mask for bindings in the "Gamepad" group.
    /// </remarks>
    [Serializable]
    public struct InputBinding : IEquatable<InputBinding>
    {
        public const char kSeparator = ';';
        public const string kSeparatorString = ";";

        /// <summary>
        /// Optional name for the binding.
        /// </summary>
        /// <remarks>
        /// For bindings that <see cref="isPartOfComposite">are part of composites</see>, this is
        /// the name of the field on the binding composite object that should be initialized with
        /// the control target of the binding.
        /// </remarks>
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>
        /// Control path being bound to.
        /// </summary>
        /// <remarks>
        /// If the binding is a composite (<see cref="isComposite"/>), the path is the composite
        /// string instead.
        /// </remarks>
        /// <example>
        /// <code>
        /// "/*/{PrimaryAction}"
        /// </code>
        /// </example>
        public string path
        {
            get { return m_Path; }
            set { m_Path = value; }
        }

        /// <summary>
        /// If the binding is overridden, this is the overriding path.
        /// Otherwise it is null.
        /// </summary>
        /// <remarks>
        /// Not serialized as overrides are considered temporary, runtime-only state.
        /// </remarks>
        public string overridePath
        {
            get { return m_OverridePath; }
            set { m_OverridePath = value; }
        }

        /// <summary>
        /// Optional list of interactions and their parameters.
        /// </summary>
        /// <example>
        /// <code>
        /// "tap,slowTap(duration=1.2)"
        /// </code>
        /// </example>
        public string interactions
        {
            get { return m_Interactions; }
            set { m_Interactions = value; }
        }

        public string overrideInteractions
        {
            get { return m_OverrideInteractions; }
            set { m_OverrideInteractions = value; }
        }

        /// <summary>
        /// Optional list of processors to apply to control values.
        /// </summary>
        /// <remarks>
        /// This list has the same format as <see cref="InputControlAttribute.processors"/>.
        /// </remarks>
        public string processors
        {
            get { return m_Processors; }
            set { m_Processors = value; }
        }

        public string overrideProcessors
        {
            get { return m_OverrideProcessors; }
            set { m_OverrideProcessors = value; }
        }

        // Optional group name. This can be used, for example, to divide bindings into
        // control schemes. So, the binding for keyboard&mouse on an action would have
        // "keyboard&mouse" as its group, the binding for "touch" would have "touch as
        // its group, and so on.
        //
        // Overriding bindings on actions that have multiple bindings is driven by
        // binding groups. This can also be used to have multiple binding for the same
        // device or control scheme and override a specific one.
        //
        // NOTE: What a group represents is not proscribed by the system. If a group is
        //       meant to represent a specific device type or combination of device types,
        //       this can be implemented on top of the system.
        //
        //       One good use case for groups is to mark up bindings that have a certain
        //       common meaning. Say, for example, you have a several binding chains
        //       (maybe even across different action sets) where the first binding in the
        //       chain always represents the same "interaction". Let's say it's the left
        //       trigger on the gamepad and it'll swap between a primary set of bindings
        //       on the four-button group on the gamepad and a secondary set. You could
        //       mark up every single use of the interaction ...
        public string groups
        {
            get { return m_Groups; }
            set { m_Groups = value; }
        }

        /// <summary>
        /// Name of the action triggered by the binding.
        /// </summary>
        /// <remarks>
        /// This is null if the binding does not trigger an action.
        ///
        /// For InputBindings that are used as filters, this can be a "mapName/actionName" combination
        /// or "mapName/*" to match all actions in the given map.
        /// </remarks>
        public string action
        {
            get { return m_Action; }
            set { m_Action = value; }
        }

        [SerializeField] private string m_Name;
        [SerializeField] private string m_Path;
        [SerializeField] private string m_Interactions;
        [SerializeField] private string m_Processors;
        [SerializeField] private string m_Groups;
        [SerializeField] private string m_Action;
        [SerializeField] internal Flags m_Flags;

        [NonSerialized] private string m_OverridePath;
        [NonSerialized] private string m_OverrideInteractions;
        [NonSerialized] private string m_OverrideProcessors;

        internal string effectivePath
        {
            get { return overridePath ?? path; }
        }

        internal string effectiveInteractions
        {
            get { return overrideInteractions ?? interactions; }
        }

        internal string effectiveProcessors
        {
            get { return overrideProcessors ?? processors; }
        }

        public bool chainWithPrevious
        {
            get { return (m_Flags & Flags.ThisAndPreviousCombine) == Flags.ThisAndPreviousCombine; }
            set
            {
                if (value)
                    m_Flags |= Flags.ThisAndPreviousCombine;
                else
                    m_Flags &= ~Flags.ThisAndPreviousCombine;
            }
        }

        public bool isComposite
        {
            get { return (m_Flags & Flags.Composite) == Flags.Composite; }
            set
            {
                if (value)
                    m_Flags |= Flags.Composite;
                else
                    m_Flags &= ~Flags.Composite;
            }
        }

        public bool isPartOfComposite
        {
            get { return (m_Flags & Flags.PartOfComposite) == Flags.PartOfComposite; }
            set
            {
                if (value)
                    m_Flags |= Flags.PartOfComposite;
                else
                    m_Flags &= ~Flags.PartOfComposite;
            }
        }

        internal bool isEmpty
        {
            get
            {
                return string.IsNullOrEmpty(effectivePath) && string.IsNullOrEmpty(action) &&
                    string.IsNullOrEmpty(groups);
            }
        }

        public bool Equals(InputBinding other)
        {
            return string.Equals(effectivePath, other.effectivePath) &&
                string.Equals(effectiveInteractions, other.effectiveInteractions) &&
                string.Equals(effectiveProcessors, other.effectiveProcessors) &&
                string.Equals(groups, other.groups) &&
                string.Equals(action, other.action);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is InputBinding && Equals((InputBinding)obj);
        }

        public static bool operator==(InputBinding left, InputBinding right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(InputBinding left, InputBinding right)
        {
            return !(left == right);
        }

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

        ////TODO: also support matching by name (taking the binding tree into account so that components
        ////      of composites can be referenced through their parent)

        internal bool Matches(ref InputBinding other)
        {
            if (path != null)
            {
                ////TODO: handle things like ignoring leading '/'
                if (other.path == null
                    || !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(path, other.path, kSeparator))
                    return false;
            }

            if (action != null)
            {
                ////TODO: handle "map/action" format
                ////TODO: handle "map/*" format
                if (other.action == null
                    || !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(action, other.action, kSeparator))
                    return false;
            }

            if (groups != null)
            {
                if (other.groups == null
                    || !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(groups, other.groups, kSeparator))
                    return false;
            }

            return true;
        }

        [Flags]
        internal enum Flags
        {
            /// <summary>
            /// This and the next binding in the list combine such that both need to be
            /// triggered to trigger the associated action.
            /// </summary>
            /// <remarks>
            /// The order in which the bindings trigger does not matter.
            ///
            /// An arbitrarily long sequence of bindings can be arranged as having to trigger
            /// together.
            ///
            /// If this is set, <see cref="ThisAndPreviousCombine"/> has to be set on the
            /// subsequent binding.
            /// </remarks>
            ThisAndNextCombine = 1 << 5,
            ThisAndNextAreExclusive = 1 << 6,

            // This binding and the previous one in the list are a combo. This one
            // can only trigger after the previous one already has.
            ThisAndPreviousCombine = 1 << 0,
            ThisAndPreviousAreExclusive = 1 << 1,

            /// <summary>
            /// Whether this binding starts a composite binding group.
            /// </summary>
            /// <remarks>
            /// This flag implies <see cref="PushBindingLevel"/>. The composite is comprised
            /// of all bindings at the same grouping level. The name of each binding in the
            /// composite is used to determine which role the resolved controls play in the
            /// composite.
            /// </remarks>
            Composite = 1 << 2,
            PartOfComposite = 1 << 3,////REVIEW: remove and replace with PushBindingLevel and PopBindingLevel?

            /// <summary>
            ///
            /// </summary>
            PushBindingLevel = 1 << 3,
            PopBindingLevel = 1 << 4,
        }
    }
}
