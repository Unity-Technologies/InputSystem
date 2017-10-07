using System;
using System.Collections.Generic;

namespace ISX
{
    // A processor that conditions values.
    // InputControls can have stacks of processors assigned to them.
    // IMPORTANT: Processors can NOT be stateful. If you need processing that requires keeping
    //            mutating state over time, use InputActions. All mutable state needs to be
    //            kept in the central state buffers.
    public interface IInputProcessor<TValue>
    {
        TValue Process(TValue value);
    }

    internal static class InputProcessor
    {
        public static Dictionary<string, Type> s_Processors;

        public static Type TryGet(string name)
        {
            Type type;
            if (s_Processors.TryGetValue(name.ToLower(), out type))
                return type;
            return null;
        }
    }
}
