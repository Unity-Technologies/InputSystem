using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Combine a single pointer with buttons and a scroll wheel.
    /// </summary>
    // IMPORTANT: State layout must match with MouseInputState in native.
    [StructLayout(LayoutKind.Explicit, Size = 30)]
    internal struct MouseState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('M', 'O', 'U', 'S');

        [InputControl(usage = "Point")]
        [FieldOffset(0)]
        public Vector2 position;

        [InputControl(usage = "Secondary2DMotion")]
        [FieldOffset(8)]
        public Vector2 delta;

        ////REVIEW: have half-axis buttons on the scroll axes? (up, down, left, right)
        [InputControl]
        [InputControl(name = "scroll/x", aliases = new[] { "horizontal" }, usage = "ScrollHorizontal", displayName = "Scroll Left/Right")]
        [InputControl(name = "scroll/y", aliases = new[] { "vertical" }, usage = "ScrollVertical", displayName = "Scroll Up/Down", shortDisplayName = "Wheel")]
        [FieldOffset(16)]
        public Vector2 scroll;

        [InputControl(name = "press", useStateFrom = "leftButton", synthetic = true, usages = new string[0])]
        [InputControl(name = "leftButton", layout = "Button", bit = (int)MouseButton.Left, usage = "PrimaryAction", displayName = "Left Button", shortDisplayName = "LMB")]
        [InputControl(name = "rightButton", layout = "Button", bit = (int)MouseButton.Right, usage = "SecondaryAction", displayName = "Right Button", shortDisplayName = "RMB")]
        [InputControl(name = "middleButton", layout = "Button", bit = (int)MouseButton.Middle, displayName = "Middle Button", shortDisplayName = "MMB")]
        [InputControl(name = "forwardButton", layout = "Button", bit = (int)MouseButton.Forward, usage = "Forward", displayName = "Forward")]
        [InputControl(name = "backButton", layout = "Button", bit = (int)MouseButton.Back, usage = "Back", displayName = "Back")]
        [FieldOffset(24)]
        // "Park" all the controls that are common to pointers but aren't use for mice such that they get
        // appended to the end of device state where they will always have default values.
        ////FIXME: InputDeviceBuilder will get fooled and set up an incorrect state layout if we don't force this to VEC2; InputControlLayout will
        ////       "infer" USHT as the format which will then end up with a layout where two 4 byte float controls are "packed" into a 16bit sized parent;
        ////       in other words, setting VEC2 here manually should *not* be necessary
        [InputControl(name = "pressure", layout = "Axis", usage = "Pressure", offset = InputStateBlock.AutomaticOffset, format = "FLT", sizeInBits = 32)]
        [InputControl(name = "twist", layout = "Axis", usage = "Twist", offset = InputStateBlock.AutomaticOffset, format = "FLT", sizeInBits = 32)]
        [InputControl(name = "radius", layout = "Vector2", usage = "Radius", offset = InputStateBlock.AutomaticOffset, format = "VEC2", sizeInBits = 64)]
        [InputControl(name = "tilt", layout = "Vector2", usage = "Tilt", offset = InputStateBlock.AutomaticOffset, format = "VEC2", sizeInBits = 64)]
        [InputControl(name = "pointerId", layout = "Digital", format = "BIT", sizeInBits = 1, offset = InputStateBlock.AutomaticOffset)] // Will stay at 0.
        public ushort buttons;

        [InputControl(layout = "Integer")]
        [FieldOffset(26)]
        public ushort displayIndex;

        [InputControl(layout = "Integer")]
        [FieldOffset(28)]
        public ushort clickCount;

        ////REVIEW: move this and the same methods in other states to extension methods?
        public MouseState WithButton(MouseButton button, bool state = true)
        {
            var bit = 1 << (int)button;
            if (state)
                buttons |= (ushort)bit;
            else
                buttons &= (ushort)~bit;
            return this;
        }

        public FourCC format
        {
            get { return kFormat; }
        }
    }

    internal enum MouseButton
    {
        Left,
        Right,
        Middle,
        Forward,
        Back
    }
}

namespace UnityEngine.InputSystem
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
    [InputControlLayout(stateType = typeof(MouseState), isGenericTypeOfDevice = true)]
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

        public ButtonControl forwardButton { get; private set; }

        public ButtonControl backButton { get; private set; }

        public IntegerControl clickCount { get; private set;  }
        /// <summary>
        /// The mouse that was added or updated last or null if there is no mouse
        /// connected to the system.
        /// </summary>
        public new static Mouse current { get; private set; }

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

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            scroll = builder.GetControl<Vector2Control>(this, "scroll");
            leftButton = builder.GetControl<ButtonControl>(this, "leftButton");
            middleButton = builder.GetControl<ButtonControl>(this, "middleButton");
            rightButton = builder.GetControl<ButtonControl>(this, "rightButton");
            forwardButton = builder.GetControl<ButtonControl>(this, "forwardButton");
            backButton = builder.GetControl<ButtonControl>(this, "backButton");
            clickCount = builder.GetControl<IntegerControl>(this, "clickCount");
            base.FinishSetup(builder);
        }

        protected new void OnNextUpdate()
        {
            base.OnNextUpdate();
            InputState.Change(scroll, Vector2.zero);
        }

        protected new unsafe void OnEvent(InputEventPtr eventPtr)
        {
            var statePtr = currentStatePtr;

            Accumulate(scroll.x, statePtr, eventPtr);
            Accumulate(scroll.y, statePtr, eventPtr);

            base.OnEvent(eventPtr);
        }

        void IInputStateCallbackReceiver.OnNextUpdate()
        {
            OnNextUpdate();
        }

        void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
        {
            OnEvent(eventPtr);
        }
    }
}
