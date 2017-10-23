using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    // IMPORTANT: Must match FingerInputState in native code.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct Touch
    {
        [FieldOffset(0)] public int touchId;
        [FieldOffset(4)] public Vector2 position;
        [FieldOffset(12)] public Vector2 delta;
        [FieldOffset(20)] public float pressure;
        [FieldOffset(24)] public Vector2 radius;
        [FieldOffset(32)] public ushort phase;
        [FieldOffset(34)] public ushort displayIndex;
    }
}
