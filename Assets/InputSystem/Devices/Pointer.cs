using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    ////REVIEW: does it really make sense to have this at the pointer level
    public enum PointerPhase
    {
        None,
        Began,
        Move,
        Ended,
        Canceled
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointerState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('P', 'T', 'R'); }
        }

        [InputControl(template = "Digital")]
        public uint pointerId;

        [InputControl(usage = "Point")]
        public Vector2 position;

        [InputControl(usage = "Secondary2DMotion", autoReset = true)]
        public Vector2 delta;

        [InputControl(template = "Analog", usage = "Pressure")]
        public float pressure;

        [InputControl(template = "Axis", usage = "Twist")]
        public float twist;

        [InputControl(template = "Vector2", usage = "Tilt")]
        public Vector2 tilt;

        [InputControl(template = "Vector2", usage = "Radius")]
        public Vector2 radius;

        [InputControl(name = "phase", template = "Digital", sizeInBits = 4)]
        [InputControl(name = "button", template = "Button", bit = 4, usages = new[] { "PrimaryAction", "PrimaryTrigger" })]
        public ushort flags;

        [InputControl(template = "Digital")]
        public ushort displayIndex;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    ////REVIEW: should this be extended to 3D?
    /// <summary>
    /// Base class for pointer-style devices where a pointer can move across a 2D surface.
    /// </summary>
    [InputState(typeof(PointerState))]
    public class Pointer : InputDevice
    {
        /// <summary>
        /// Current position of the pointer on its 2D surface.
        /// </summary>
        /// <remarks>
        /// The coordinates are not normalized.
        /// </remarks>
        public Vector2Control position { get; private set; }
        public Vector2Control delta { get; private set; }
        public Vector2Control tilt { get; private set; }
        public Vector2Control radius { get; private set; }
        public AxisControl pressure { get; private set; }
        public AxisControl twist { get; private set; }
        public DiscreteControl pointerId { get; private set; }
        ////TODO: find a way which gives values as PointerPhase instead of as int
        public DiscreteControl phase { get; private set; }
        public DiscreteControl displayIndex { get; private set; }
        public ButtonControl button { get; private set; }

        public static Pointer current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            position = setup.GetControl<Vector2Control>(this, "position");
            delta = setup.GetControl<Vector2Control>(this, "delta");
            tilt = setup.GetControl<Vector2Control>(this, "tilt");
            radius = setup.GetControl<Vector2Control>(this, "radius");
            pressure = setup.GetControl<AxisControl>(this, "pressure");
            twist = setup.GetControl<AxisControl>(this, "twist");
            pointerId = setup.GetControl<DiscreteControl>(this, "pointerId");
            phase = setup.GetControl<DiscreteControl>(this, "phase");
            displayIndex = setup.GetControl<DiscreteControl>(this, "displayIndex");
            button = setup.GetControl<ButtonControl>(this, "button");
            base.FinishSetup(setup);
        }
    }
}
