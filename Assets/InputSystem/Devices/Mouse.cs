using System.Runtime.InteropServices;
using ISX.LowLevel;
using ISX.Utilities;
using UnityEngine;

namespace ISX
{
    /// <summary>
    /// Combine a single pointer with buttons and a scroll wheel.
    /// </summary>
    // IMPORTANT: State layout must match with MouseInputState in native.
    [StructLayout(LayoutKind.Explicit, Size = 28)]
    public struct MouseState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('M', 'O', 'U', 'S'); }
        }

        [InputControl(usage = "Point")]
        [FieldOffset(0)]
        public Vector2 position;

        [InputControl(usage = "Secondary2DMotion", autoReset = true)]
        [FieldOffset(8)]
        public Vector2 delta;

        [InputControl]
        [InputControl(name = "scroll/x", aliases = new[] { "horizontal" }, usage = "ScrollHorizontal")]
        [InputControl(name = "scroll/y", aliases = new[] { "vertical" }, usage = "ScrollVertical")]
        [FieldOffset(16)]
        public Vector2 scroll;

        [InputControl(name = "leftButton", template = "Button", bit = (int)Button.Left, alias = "button", usages = new[] { "PrimaryAction", "PrimaryTrigger" })]
        [InputControl(name = "rightButton", template = "Button", bit = (int)Button.Right, usages = new[] { "SecondaryAction", "SecondaryTrigger" })]
        [InputControl(name = "middleButton", template = "Button", bit = (int)Button.Middle)]
        [FieldOffset(24)]
        // "Park" all the controls that are common to pointers but aren't use for mice such that they get
        // appended to the end of device state where they will always have default values.
        [InputControl(name = "pressure", template = "Axis", usage = "Pressure", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "twist", template = "Axis", usage = "Twist", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "radius", template = "Vector2", usage = "Radius", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "tilt", template = "Vector2", usage = "Tilt", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "pointerId", template = "Digital", offset = InputStateBlock.kInvalidOffset)] // Will stay at 0.
        [InputControl(name = "phase", template = "Digital", offset = InputStateBlock.kInvalidOffset)] ////REVIEW: should this make use of None and Moved?
        public ushort buttons;

        [InputControl(template = "Digital")]
        [FieldOffset(26)]
        public ushort displayIndex;

        public enum Button
        {
            Left,
            Right,
            Middle,
            Forward,
            Back
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    /// <summary>
    /// A mouse input device.
    /// </summary>
    /// <remarks>
    /// Adds a scroll wheel and a typical 3-button setup with a left, middle, and right
    /// button.
    ///
    /// To control cursor display and behavior, use <see cref="UnityEngine.Cursor"/>.
    /// </remarks>
    [InputState(typeof(MouseState))]
    public class Mouse : Pointer
    {
        /// <summary>
        /// The horizontal and vertical scroll wheels.
        /// </summary>
        public Vector2Control scroll { get; private set; }

        /// <summary>
        /// The left mouse button.
        /// </summary>
        public ButtonControl leftButton { get; private set; }

        /// <summary>
        /// The middle mouse button.
        /// </summary>
        public ButtonControl middleButton { get; private set; }

        /// <summary>
        /// The right mouse button.
        /// </summary>
        public ButtonControl rightButton { get; private set; }

        /// <summary>
        /// The mouse that was added or updated last or null if there is no mouse
        /// connected to the system.
        /// </summary>
        public new static Mouse current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            scroll = setup.GetControl<Vector2Control>(this, "scroll");
            leftButton = setup.GetControl<ButtonControl>(this, "leftButton");
            middleButton = setup.GetControl<ButtonControl>(this, "middleButton");
            rightButton = setup.GetControl<ButtonControl>(this, "rightButton");
            base.FinishSetup(setup);
        }
    }
}
