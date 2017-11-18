using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    // IMPORTANT: Must match with PenInputState in native.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct PenState
    {
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

        [InputControl(name = "phase", template = "Digital", sizeInBits = 4)]
        [InputControl(name = "leftButton", template = "Button", bit = (int)Button.Left, alias = "button", usages = new[] { "PrimaryAction", "PrimaryTrigger" })]
        [InputControl(name = "rightButton", template = "Button", bit = (int)Button.Right, usages = new[] { "SecondaryAction", "SecondaryTrigger" })]
        [InputControl(name = "middleButton", template = "Button", bit = (int)Button.Middle)]
        // "Park" unused controls.
        [InputControl(name = "radius", template = "Vector2", usage = "Radius", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "pointerId", template = "Digital", offset = InputStateBlock.kInvalidOffset)] // Will stay at 0.
        [FieldOffset(32)]
        public ushort buttons;

        [InputControl(template = "Digital")]
        [FieldOffset(34)]
        public ushort displayIndex;

        public enum Button
        {
            Left = 4,
            Right = 5,
            Middle = 6,
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
