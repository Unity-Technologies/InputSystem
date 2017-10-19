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

    // NOTE: This layout has to match the PointerInputState layout used in native!
    [StructLayout(LayoutKind.Sequential)]
    public struct PointerState
    {
        public static FourCC kFormat => new FourCC('P', 'T', 'R');

        // There's systems where 0 is a valid finger ID. Should add +1 to system IDs
        // in that case.
        public const uint kInvalidPointerId = 0;

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

        [InputControl(template = "Vector2")]
        [InputControl(name = "scroll/x", aliases = new[] { "horizontal" }, usage = "ScrollHorizontal")]
        [InputControl(name = "scroll/y", aliases = new[] { "vertical" }, usage = "ScrollVertical")]
        public Vector2 scroll;
    }

    [InputState(typeof(PointerState))]
    public class Pointer : InputDevice
    {
        public static Pointer current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }
    }
}
