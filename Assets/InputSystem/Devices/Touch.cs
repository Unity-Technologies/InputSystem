using System.Runtime.InteropServices;

namespace ISX
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TouchscreenState
    {
        public const int kMaxFingers = 10;

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
    }
}
