using System;

////TODO: rename "combining" to "chaining"

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A combination of a control path and modifiers.
    /// </summary>
    /// <remarks>
    /// A single binding can match arbitrary many controls through its path.
    /// </remarks>
    [Serializable]
    public struct InputBinding
    {
        [Flags]
        public enum Flags
        {
            // This binding and the previous one in the list are a combo. This one
            // can only trigger after the previous one already has.
            ThisAndPreviousCombine = 1 << 0,

            Composite = 1 << 1,
            PartOfComposite = 1 << 2,
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
        /// Optional list of modifiers and their parameters.
        /// </summary>
        /// <example>
        /// <code>
        /// "tap,slowTap(duration=1.2)"
        /// </code>
        /// </example>
        public string modifiers;

        ////TODO: allow more than one group
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
        //       chain always represents the same "modifier". Let's say it's the left
        //       trigger on the gamepad and it'll swap between a primary set of bindings
        //       on the four-button group on the gamepad and a secondary set. You could
        //       mark up every single use of the modifier ...
        ////REVIEW: this almost begs for a hierarchy of bindings...
        public string group;

        public Flags flags;

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
    }
}
