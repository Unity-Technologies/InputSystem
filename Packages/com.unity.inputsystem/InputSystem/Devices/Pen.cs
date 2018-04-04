using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: we need editor window space conversion on the pen, too

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Default state layout for pen devices.
    /// </summary>
    // IMPORTANT: Must match with PenInputState in native.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct PenState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('P', 'E', 'N'); }
        }

        [InputControl(usage = "Point")]
        [FieldOffset(0)]
        public Vector2 position;

        [InputControl(usage = "Secondary2DMotion")]
        [FieldOffset(8)]
        public Vector2 delta;

        [InputControl(layout = "Vector2", usage = "Tilt")]
        [FieldOffset(16)]
        public Vector2 tilt;

        [InputControl(layout = "Analog", usage = "Pressure")]
        [FieldOffset(24)]
        public float pressure;

        [InputControl(layout = "Axis", usage = "Twist")]
        [FieldOffset(28)]
        public float twist;

        [InputControl(name = "tip", layout = "Button", bit = (int)Button.Tip, alias = "button")]
        [InputControl(name = "eraser", layout = "Button", bit = (int)Button.Eraser)]
        [InputControl(name = "barrelFirst", layout = "Button", bit = (int)Button.BarrelFirst, alias = "button", usages = new[] { "PrimaryAction", "PrimaryTrigger" })]
        [InputControl(name = "barrelSecond", layout = "Button", bit = (int)Button.BarrelSecond, usages = new[] { "SecondaryAction", "SecondaryTrigger" })]
        // "Park" unused controls.
        [InputControl(name = "radius", layout = "Vector2", usage = "Radius", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "pointerId", layout = "Digital", offset = InputStateBlock.kInvalidOffset)] // Will stay at 0.
        [InputControl(name = "phase", layout = "PointerPhase", offset = InputStateBlock.kInvalidOffset)] ////TODO: this should be used
        [FieldOffset(32)]
        public ushort buttons;

        [InputControl(layout = "Digital")]
        [FieldOffset(34)]
        public ushort displayIndex;

        public enum Button
        {
            Tip,
            Eraser,
            BarrelFirst,
            BarrelSecond
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A pen/stylus input device.
    /// </summary>
    [InputLayout(stateType = typeof(PenState))]
    public class Pen : Pointer
    {
        public new static Pen current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }
    }
}
