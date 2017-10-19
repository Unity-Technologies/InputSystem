using System.Runtime.InteropServices;

namespace ISX
{
    // Combine multiple pointers each corresponding to a finger.
    // All fingers combine to quite a bit of state; ideally send delta events that update
    // only specific fingers.
    [StructLayout(LayoutKind.Sequential)]
    public struct TouchscreenState
    {
        public static FourCC kFormat => new FourCC('T', 'O', 'U', 'C');

        public const int kMaxFingers = 10;

        ////REVIEW: shouldn't these all be controls?

        public PointerState finger1;
        public PointerState finger2;
        public PointerState finger3;
        public PointerState finger4;
        public PointerState finger5;
        public PointerState finger6;
        public PointerState finger7;
        public PointerState finger8;
        public PointerState finger9;
        public PointerState finger10;

        public unsafe PointerState* fingers
        {
            get
            {
                fixed(PointerState * ptr = &finger1)
                {
                    return ptr;
                }
            }
        }
    }

    [InputState(typeof(TouchscreenState))]
    public class Touchscreen : Pointer
    {
        public new static Touchscreen current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }
    }
}
