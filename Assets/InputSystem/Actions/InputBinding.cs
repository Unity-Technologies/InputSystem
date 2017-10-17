using System;

namespace ISX
{
    [Serializable]
    public struct InputBinding
    {
        [Flags]
        public enum Flags
        {
            // This binding and the next one in the list are a combo. The next
            // one can't trigger until this binding triggers.
            ThisAndNextCombine = 1 << 0,
        }

        public string path;
        public string modifiers;
        public string processors;////TODO
        public Flags flags;
    }
}
