using System.Runtime.InteropServices;

namespace ISX
{
    // Combine multiple pointers each corresponding to a finger.
    // All fingers combine to quite a bit of state; ideally send delta events that update
    // only specific fingers.
    [StructLayout(LayoutKind.Sequential)]
    public struct TouchscreenState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('T', 'O', 'U', 'C');

        public const int kMaxFingers = 10;

        ////REVIEW: shouldn't these all be controls?

        /*
        [InputControl] public PointerState finger1;
        [InputControl] public PointerState finger2;
        [InputControl] public PointerState finger3;
        [InputControl] public PointerState finger4;
        [InputControl] public PointerState finger5;
        [InputControl] public PointerState finger6;
        [InputControl] public PointerState finger7;
        [InputControl] public PointerState finger8;
        [InputControl] public PointerState finger9;
        [InputControl] public PointerState finger10;

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
        */

        public FourCC GetFormat()
        {
            return kFormat;
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
