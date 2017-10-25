using System;

namespace ISX
{
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

        public string path;
        public string modifiers;
        public string processors;////TODO
        public Flags flags;

        public bool combinesWithPrevious
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
