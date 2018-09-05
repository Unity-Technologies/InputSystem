using System;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Serialization;

////REVIEW: should bindings have unique IDs, too? maybe instead of "name"?

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
    /// </remarks>
    [Serializable]
    public struct InputBinding
    {
        public const char kSeparator = ';';
        public const string kSeparatorString = ";";

        [Flags]
        public enum Flags
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

        /// <summary>
        /// Optional name for the binding.
        /// </summary>
        /// <remarks>
        /// For bindings that <see cref="isPartOfComposite">are part of composites</see>, this is
        /// the name of the field on the binding composite object that should be initialized with
        /// the control target of the binding.
        /// </remarks>
        public string name;

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
        public string path;

        /// <summary>
        /// If the binding is overridden, this is the overriding path.
        /// Otherwise it is null.
        /// </summary>
        /// <remarks>
        /// Not serialized as overrides are considered temporary, runtime-only state.
        /// </remarks>
        [NonSerialized] public string overridePath;

        /// <summary>
        /// Optional list of interactions and their parameters.
        /// </summary>
        /// <example>
        /// <code>
        /// "tap,slowTap(duration=1.2)"
        /// </code>
        /// </example>
        [FormerlySerializedAs("modifiers")]
        public string interactions;

        [NonSerialized] public string overrideInteractions;

        /// <summary>
        /// Optional list of processors to apply to control values.
        /// </summary>
        /// <remarks>
        /// This list has the same format as <see cref="InputControlAttribute.processors"/>.
        /// </remarks>
        public string processors;

        [NonSerialized] public string overrideProcessors;

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
        public string groups;

        /// <summary>
        /// Name of the action triggered by the binding.
        /// </summary>
        /// <remarks>
        /// This is null if the binding does not trigger an action.
        /// </remarks>
        public string action;

        public Flags flags;

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
            get { return (flags & Flags.ThisAndPreviousCombine) == Flags.ThisAndPreviousCombine; }
            set
            {
                if (value)
                    flags |= Flags.ThisAndPreviousCombine;
                else
                    flags &= ~Flags.ThisAndPreviousCombine;
            }
        }

        public bool isComposite
        {
            get { return (flags & Flags.Composite) == Flags.Composite; }
            set
            {
                if (value)
                    flags |= Flags.Composite;
                else
                    flags &= ~Flags.Composite;
            }
        }

        public bool isPartOfComposite
        {
            get { return (flags & Flags.PartOfComposite) == Flags.PartOfComposite; }
            set
            {
                if (value)
                    flags |= Flags.PartOfComposite;
                else
                    flags &= ~Flags.PartOfComposite;
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
    }
}
