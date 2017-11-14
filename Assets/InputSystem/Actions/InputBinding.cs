using System;

////TODO: rename "combining" to "chaining"

namespace ISX
{
    // A combination of a control path and modifiers.
    // A single binding can match arbitrary many controls through its path.
    [Serializable]
    public struct InputBinding
    {
        [Flags]
        public enum Flags
        {
            // This binding and the previous one in the list are a combo. This one
            // can only trigger after the previous one already has.
            ThisAndPreviousCombine = 1 << 0,
        }

        // Control path.
        // Example: "/*/{PrimaryAction}"
        public string path;

        // If the binding is overridden, this is the overriding path.
        // Otherwise it is null.
        // NOTE: Not serialized as overrides are considered temporary, runtime-only state.
        [NonSerialized] public string overridePath;

        // Modifier list.
        // Example: "tap,slowTap(duration=1.2)"
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
    }
}
