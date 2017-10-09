using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    public enum PointerPhase
    {
        None,
        Began,
        Move,
        Finished,
        Canceled
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct PointerState
    {
        public const uint kInvalidPointerId = 0xffffffff;

        [InputControl(template = "Digital")]
        public uint pointerId;

        [InputControl(template = "Digital")]
        public PointerPhase phase;

        [InputControl(template = "Analog", usage = "Pressure")]
        public float pressure;

        [InputControl(template = "Vector2", usage = "Point")]
        public Vector2 position;

        // IMPORTANT: Accumulation and *resetting* (i.e. going back to zero in-between frames)
        //            has to be done by the code that generates state events. The system will *not*
        //            automatically maintain deltas.
        [InputControl(usage = "secondaryStick")]
        public Vector2 delta;

        [InputControl(usage = "Scroll")]
        ////TODO: modifying subcontrols like this doesn't work yet
        //[InputControl(name = "scroll/x", aliases = new[] { "horizontal" }, usage = "ScrollHorizontal")]
        //[InputControl(name = "scroll/y", aliases = new[] { "vertical" }, usage = "ScrollVertical")]
        public Vector2 scroll;
    }

    [InputState(typeof(PointerState))]
    public class Pointer : InputDevice
    {
        public static Pointer current { get; protected set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }
    }
}
