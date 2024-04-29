using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    readonly struct Usage
    {
        [FieldOffset(0)] public readonly uint value;
    }
    
    struct Usages
    {
        struct Interface
        {
            public const uint Gamepad = 4;
        }
    }
}