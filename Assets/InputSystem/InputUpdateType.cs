using System;

namespace ISX
{
    [Flags]
    public enum InputUpdateType
    {
        Dynamic = 1 << 0,
        Fixed = 1 << 1,
        BeforeRender = 1 << 2,
        Editor = 1 << 3
    }
}