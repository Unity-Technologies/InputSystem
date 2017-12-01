using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
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

        [InputControl(usage = "Secondary2DMotion", autoReset = true)]
        [FieldOffset(8)]
        public Vector2 delta;

        [InputControl(template = "Vector2", usage = "Tilt")]
        [FieldOffset(16)]
        public Vector2 tilt;

        [InputControl(template = "Analog", usage = "Pressure")]
        [FieldOffset(24)]
        public float pressure;

        [InputControl(template = "Axis", usage = "Twist")]
        [FieldOffset(28)]
        public float twist;

        [InputControl(name = "tip", template = "Button", bit = (int)Button.Tip, alias = "button")]
        [InputControl(name = "eraser", template = "Button", bit = (int)Button.Eraser)]
        [InputControl(name = "barrelFirst", template = "Button", bit = (int)Button.BarrelFirst, alias = "button", usages = new[] { "PrimaryAction", "PrimaryTrigger" })]
        [InputControl(name = "barrelSecond", template = "Button", bit = (int)Button.BarrelSecond, usages = new[] { "SecondaryAction", "SecondaryTrigger" })]
        // "Park" unused controls.
        [InputControl(name = "radius", template = "Vector2", usage = "Radius", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "pointerId", template = "Digital", offset = InputStateBlock.kInvalidOffset)] // Will stay at 0.
        [InputControl(name = "phase", template = "Digital", offset = InputStateBlock.kInvalidOffset)] ////TODO: this should be used
        [FieldOffset(32)]
        public ushort buttons;

        [InputControl(template = "Digital")]
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

    [InputState(typeof(PenState))]
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
