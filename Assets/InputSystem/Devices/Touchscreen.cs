using System.Runtime.InteropServices;

namespace ISX
{
    // Combine multiple pointers each corresponding to a finger.
    // All fingers combine to quite a bit of state; ideally send delta events that update
    // only specific fingers.
    //
    // IMPORTANT: Must match TouchInputState in native code.
    [StructLayout(LayoutKind.Explicit, Size = 360)]
    public struct TouchscreenState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('T', 'S', 'C', 'R'); }
        }

        public const int kMaxTouches = 10;

        [InputControl(template = "Touch")]
        // Add controls compatible with what Pointer expects and redirect their
        // state to the state of touch0 so that this essentially becomes our
        // pointer control.
        // NOTE: Some controls from Pointer don't make sense for touch and we "park"
        //       them by assigning them invalid offsets (thus having automatic state
        //       layout put them at the end of our fixed state).
        [InputControl(name = "fingerId", template = "Digital", alias = "pointerId", useStateFrom = "touch0/touchId")]
        [InputControl(name = "position", template = "Vector2", usage = "Point", useStateFrom = "touch0/position")]
        [InputControl(name = "delta", template = "Vector2", usage = "Secondary2DMotion", useStateFrom = "touch0/delta")]
        [InputControl(name = "pressure", template = "Axis", usage = "Pressure", useStateFrom = "touch0/pressure")]
        [InputControl(name = "radius", template = "Vector2", usage = "Radius", useStateFrom = "touch0/radius")]
        [InputControl(name = "phase", template = "Digital", useStateFrom = "touch0/phase")]
        [InputControl(name = "displayIndex", template = "Digital", useStateFrom = "touch0/displayIndex")]
        [InputControl(name = "twist", template = "Axis", usage = "Twist", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "tilt", template = "Vector2", usage = "Tilt", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "button", template = "Button", usages = new[] { "PrimaryAction", "PrimaryTrigger" }, offset = InputStateBlock.kInvalidOffset)]
        [FieldOffset(0)]
        public Touch touch0;

        [InputControl(template = "Touch")]
        [FieldOffset(36)]
        public Touch touch1;

        [InputControl(template = "Touch")]
        [FieldOffset(72)]
        public Touch touch2;

        [InputControl(template = "Touch")]
        [FieldOffset(108)]
        public Touch touch3;

        [InputControl(template = "Touch")]
        [FieldOffset(144)]
        public Touch touch4;

        [InputControl(template = "Touch")]
        [FieldOffset(180)]
        public Touch touch5;

        [InputControl(template = "Touch")]
        [FieldOffset(216)]
        public Touch touch6;

        [InputControl(template = "Touch")]
        [FieldOffset(252)]
        public Touch touch7;

        [InputControl(template = "Touch")]
        [FieldOffset(288)]
        public Touch touch8;

        [InputControl(template = "Touch")]
        [FieldOffset(324)]
        public Touch touch9;

        public unsafe Touch* touches
        {
            get
            {
                fixed(Touch * ptr = &touch0)
                {
                    return ptr;
                }
            }
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [InputState(typeof(TouchscreenState))]
    public class Touchscreen : Pointer
    {
        public ReadOnlyArray<TouchControl> touches { get; private set; }

        public new static Touchscreen current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            var touchArray = new TouchControl[TouchscreenState.kMaxTouches];

            touchArray[0] = setup.GetControl<TouchControl>(this, "touch0");
            touchArray[1] = setup.GetControl<TouchControl>(this, "touch1");
            touchArray[2] = setup.GetControl<TouchControl>(this, "touch2");
            touchArray[3] = setup.GetControl<TouchControl>(this, "touch3");
            touchArray[4] = setup.GetControl<TouchControl>(this, "touch4");
            touchArray[5] = setup.GetControl<TouchControl>(this, "touch5");
            touchArray[6] = setup.GetControl<TouchControl>(this, "touch6");
            touchArray[7] = setup.GetControl<TouchControl>(this, "touch7");
            touchArray[8] = setup.GetControl<TouchControl>(this, "touch8");
            touchArray[9] = setup.GetControl<TouchControl>(this, "touch9");

            touches = new ReadOnlyArray<TouchControl>(touchArray);

            base.FinishSetup(setup);
        }
    }
}
