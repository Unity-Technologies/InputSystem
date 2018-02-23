using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    // IMPORTANT: Must match FingerInputState in native code.
    // IMPORTANT: TouchControl is hardwired to the layout of this struct.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct Touch
    {
        [FieldOffset(0)] public int touchId;
        [FieldOffset(4)] public Vector2 position;
        [FieldOffset(12)] public Vector2 delta;
        [FieldOffset(20)] public float pressure;
        [FieldOffset(24)] public Vector2 radius;
        [FieldOffset(32)] public ushort phaseId;
        [FieldOffset(34)] public ushort displayIndex;

        public PointerPhase phase
        {
            get { return (PointerPhase)phaseId; }
        }
    }
}
