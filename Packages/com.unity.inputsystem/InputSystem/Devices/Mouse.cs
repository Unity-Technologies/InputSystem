using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
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

        [InputControl(usage = "Secondary2DMotion")]
        [FieldOffset(8)]
        public Vector2 delta;

        [InputControl]
        [InputControl(name = "scroll/x", aliases = new[] { "horizontal" }, usage = "ScrollHorizontal")]
        [InputControl(name = "scroll/y", aliases = new[] { "vertical" }, usage = "ScrollVertical")]
        [FieldOffset(16)]
        public Vector2 scroll;

        [InputControl(name = "leftButton", layout = "Button", bit = (int)Button.Left, alias = "button", usages = new[] { "PrimaryAction", "PrimaryTrigger" })]
        [InputControl(name = "rightButton", layout = "Button", bit = (int)Button.Right, usages = new[] { "SecondaryAction", "SecondaryTrigger" })]
        [InputControl(name = "middleButton", layout = "Button", bit = (int)Button.Middle)]
        [FieldOffset(24)]
        // "Park" all the controls that are common to pointers but aren't use for mice such that they get
        // appended to the end of device state where they will always have default values.
        [InputControl(name = "pressure", layout = "Axis", usage = "Pressure", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "twist", layout = "Axis", usage = "Twist", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "radius", layout = "Vector2", usage = "Radius", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "tilt", layout = "Vector2", usage = "Tilt", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "pointerId", layout = "Digital", format = "BIT", sizeInBits = 1, offset = InputStateBlock.kInvalidOffset)] // Will stay at 0.
        [InputControl(name = "phase", layout = "PointerPhase", format = "BIT", sizeInBits = 4, offset = InputStateBlock.kInvalidOffset)] ////REVIEW: should this make use of None and Moved?
        public ushort buttons;

        [InputControl(layout = "Digital")]
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

        ////REVIEW: move this and the same methods in other states to extension methods?
        public MouseState WithButton(Button button, bool state = true)
        {
            var bit = 1 << (int)button;
            if (state)
                buttons |= (ushort)bit;
            else
                buttons &= (ushort)~bit;
            return this;
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
    /// A mouse input device.
    /// </summary>
    /// <remarks>
    /// Adds a scroll wheel and a typical 3-button setup with a left, middle, and right
    /// button.
    ///
    /// To control cursor display and behavior, use <see cref="UnityEngine.Cursor"/>.
    /// </remarks>
    [InputControlLayout(stateType = typeof(MouseState))]
    public class Mouse : Pointer, IInputStateCallbackReceiver
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

        ////REVIEW: how should we handle this being called from EditorWindow's? (where the editor window space processor will turn coordinates automatically into editor window space)
        /// <summary>
        /// Move the operating system's mouse cursor.
        /// </summary>
        /// <param name="position">New position in player window space.</param>
        /// <remarks>
        /// The <see cref="Pointer.position"/> property will not update immediately but rather will update in the
        /// next input update.
        /// </remarks>
        public void WarpCursorPosition(Vector2 position)
        {
            var command = WarpMousePositionCommand.Create(position);
            ExecuteCommand(ref command);
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            scroll = builder.GetControl<Vector2Control>(this, "scroll");
            leftButton = builder.GetControl<ButtonControl>(this, "leftButton");
            middleButton = builder.GetControl<ButtonControl>(this, "middleButton");
            rightButton = builder.GetControl<ButtonControl>(this, "rightButton");
            base.FinishSetup(builder);
        }

        bool IInputStateCallbackReceiver.OnCarryStateForward(IntPtr statePtr)
        {
            var deltaXChanged = ResetDelta(statePtr, delta.x);
            var deltaYChanged = ResetDelta(statePtr, delta.y);
            var scrollXChanged = ResetDelta(statePtr, scroll.x);
            var scrollYChanged = ResetDelta(statePtr, scroll.y);
            return deltaXChanged || deltaYChanged || scrollXChanged || scrollYChanged;
        }

        void IInputStateCallbackReceiver.OnBeforeWriteNewState(IntPtr oldStatePtr, IntPtr newStatePtr)
        {
            AccumulateDelta(oldStatePtr, newStatePtr, delta.x);
            AccumulateDelta(oldStatePtr, newStatePtr, delta.y);
            AccumulateDelta(oldStatePtr, newStatePtr, scroll.x);
            AccumulateDelta(oldStatePtr, newStatePtr, scroll.y);
        }
    }

    //can we have a structure for doing those different simulation parts in a controlled fashion?

    /// <summary>
    /// Simulate mouse input from touch or gamepad input.
    /// </summary>
    public class MouseSimulation
    {
        /// <summary>
        /// Whether to translate touch input into mouse input.
        /// </summary>
        public bool useTouchInput
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Whether to translate gamepad and joystick input into mouse input.
        /// </summary>
        public bool useControllerInput
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public static MouseSimulation instance
        {
            get { throw new NotImplementedException(); }
        }

        public void Enable()
        {
            throw new NotImplementedException();
        }

        public void Disable()
        {
            throw new NotImplementedException();
        }

        private Mouse m_Mouse;
    }
}
