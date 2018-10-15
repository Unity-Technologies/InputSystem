using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Haptics;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: come up with consistent naming for buttons; (xxxButton? xxx?)

////REVIEW: should we add a gyro as a standard feature of gamepads?

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Default state layout for gamepads.
    /// </summary>
    /// <seealso cref="Gamepad"/>
    // NOTE: Must match GamepadInputState in native.
    [StructLayout(LayoutKind.Explicit, Size = 28)]
    public struct GamepadState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('G', 'P', 'A', 'D'); }
        }

        /// <summary>
        /// Button bit mask.
        /// </summary>
        /// <seealso cref="Button"/>
        ////REVIEW: do we want the name to correspond to what's actually on the device?
        [InputControl(name = "dpad", layout = "Dpad", usage = "Hatswitch")]
        [InputControl(name = "buttonSouth", layout = "Button", bit = (uint)Button.South, usage = "PrimaryAction", aliases = new[] { "a", "cross" })]
        [InputControl(name = "buttonWest", layout = "Button", bit = (uint)Button.West, usage = "SecondaryAction", aliases = new[] { "x", "square" })]
        [InputControl(name = "buttonNorth", layout = "Button", bit = (uint)Button.North, aliases = new[] { "y", "triangle" })]
        [InputControl(name = "buttonEast", layout = "Button", bit = (uint)Button.East, usage = "Back", aliases = new[] { "b", "circle" })]
        ////FIXME: 'Press' naming is inconsistent with 'Button' naming
        [InputControl(name = "leftStickPress", layout = "Button", bit = (uint)Button.LeftStick)]
        [InputControl(name = "rightStickPress", layout = "Button", bit = (uint)Button.RightStick)]
        [InputControl(name = "leftShoulder", layout = "Button", bit = (uint)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", layout = "Button", bit = (uint)Button.RightShoulder)]
        ////REVIEW: seems like these two should get less ambiguous names as well
        [InputControl(name = "start", layout = "Button", bit = (uint)Button.Start, usage = "Menu")]
        [InputControl(name = "select", layout = "Button", bit = (uint)Button.Select)]
        [FieldOffset(0)]
        public uint buttons;

        /// <summary>
        /// Left stick position.
        /// </summary>
        [InputControl(variants = "Default", layout = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        [InputControl(variants = "Lefty", layout = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        [FieldOffset(4)]
        public Vector2 leftStick;

        /// <summary>
        /// Right stick position.
        /// </summary>
        [InputControl(variants = "Default", layout = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        [InputControl(variants = "Lefty", layout = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        [FieldOffset(12)]
        public Vector2 rightStick;

        /// <summary>
        /// Position of the left trigger.
        /// </summary>
        [InputControl(variants = "Default", layout = "Button", format = "FLT", usage = "SecondaryTrigger")]
        [InputControl(variants = "Lefty", layout = "Button", format = "FLT", usage = "PrimaryTrigger")]
        [FieldOffset(20)]
        public float leftTrigger;

        /// <summary>
        /// Position of the right trigger.
        /// </summary>
        [InputControl(variants = "Default", layout = "Button", format = "FLT", usage = "PrimaryTrigger")]
        [InputControl(variants = "Lefty", layout = "Button", format = "FLT", usage = "SecondaryTrigger")]
        [FieldOffset(24)]
        public float rightTrigger;

        public enum Button
        {
            // Dpad buttons. Important to be first in the bitfield as we'll
            // point the DpadControl to it.
            // IMPORTANT: Order has to match what is expected by DpadControl.
            DpadUp,
            DpadDown,
            DpadLeft,
            DpadRight,

            // Face buttons. We go with a north/south/east/west naming as that
            // clearly disambiguates where we expect the respective button to be.
            North,
            East,
            South,
            West,

            LeftStick,
            RightStick,
            LeftShoulder,
            RightShoulder,

            Start,
            Select,

            // Aliases Xbox style.
            X = West,
            Y = North,
            A = South,
            B = East,

            // Aliases PS4 style.
            Cross = South,
            Square = West,
            Triangle = North,
            Circle = East,
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }

        public GamepadState WithButton(Button button, bool value = true)
        {
            var bit = (uint)1 << (int)button;
            if (value)
                buttons |= bit;
            else
                buttons &= ~bit;
            return this;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// An Xbox-style gamepad with two switcks, a D-Pad, four face buttons, two triggers,
    /// two shoulder buttons, and two menu buttons.
    /// </summary>
    [InputControlLayout(stateType = typeof(GamepadState))]
    public class Gamepad : InputDevice, IDualMotorRumble
    {
        ////REVIEW: add PS4 and Xbox style alternate accessors?
        public ButtonControl buttonWest { get; private set; }
        public ButtonControl buttonNorth { get; private set; }
        public ButtonControl buttonSouth { get; private set; }
        public ButtonControl buttonEast { get; private set; }

        public ButtonControl leftStickButton { get; private set; }
        public ButtonControl rightStickButton { get; private set; }

        public ButtonControl startButton { get; private set; }
        public ButtonControl selectButton { get; private set; }

        public DpadControl dpad { get; private set; }

        public ButtonControl leftShoulder { get; private set; }
        public ButtonControl rightShoulder { get; private set; }

        public StickControl leftStick { get; private set; }
        public StickControl rightStick { get; private set; }

        public ButtonControl leftTrigger { get; private set; }
        public ButtonControl rightTrigger { get; private set; }

        ////TODO: noise filtering
        /// <summary>
        /// The gamepad last used by the user or null if there is no gamepad connected to the system.
        /// </summary>
        public static Gamepad current { get; internal set; }

        /// <summary>
        /// A list of gamepads currently connected to the system.
        /// </summary>
        /// <remarks>
        /// Does not cause GC allocation.
        ///
        /// Do *NOT* hold on to the value returned by this getter but rather query it whenever
        /// you need it. Whenever the gamepad setup changes, the value returned by this getter
        /// is invalidated.
        /// </remarks>
        public static ReadOnlyArray<Gamepad> all
        {
            get { return new ReadOnlyArray<Gamepad>(s_Gamepads, 0, s_GamepadCount); }
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            buttonWest = builder.GetControl<ButtonControl>(this, "buttonWest");
            buttonNorth = builder.GetControl<ButtonControl>(this, "buttonNorth");
            buttonSouth = builder.GetControl<ButtonControl>(this, "buttonSouth");
            buttonEast = builder.GetControl<ButtonControl>(this, "buttonEast");

            startButton = builder.GetControl<ButtonControl>(this, "start");
            selectButton = builder.GetControl<ButtonControl>(this, "select");

            leftStickButton = builder.GetControl<ButtonControl>(this, "leftStickPress");
            rightStickButton = builder.GetControl<ButtonControl>(this, "rightStickPress");

            dpad = builder.GetControl<DpadControl>(this, "dpad");

            leftShoulder = builder.GetControl<ButtonControl>(this, "leftShoulder");
            rightShoulder = builder.GetControl<ButtonControl>(this, "rightShoulder");

            leftStick = builder.GetControl<StickControl>(this, "leftStick");
            rightStick = builder.GetControl<StickControl>(this, "rightStick");

            leftTrigger = builder.GetControl<ButtonControl>(this, "leftTrigger");
            rightTrigger = builder.GetControl<ButtonControl>(this, "rightTrigger");

            base.FinishSetup(builder);
        }

        protected override void RefreshConfiguration()
        {
            base.RefreshConfiguration();
            RefreshUserId();
        }

        protected override void OnAdded()
        {
            ArrayHelpers.AppendWithCapacity(ref s_Gamepads, ref s_GamepadCount, this);
        }

        protected override void OnRemoved()
        {
            if (current == this)
                current = null;

            // Remove from array.
            var wasFound = ArrayHelpers.Erase(ref s_Gamepads, this);
            Debug.Assert(wasFound, string.Format("Gamepad {0} seems to not have been added but is being removed", this));
            if (wasFound)
                --s_GamepadCount;
        }

        public virtual void PauseHaptics()
        {
            m_Rumble.PauseHaptics(this);
        }

        public virtual void ResumeHaptics()
        {
            m_Rumble.ResumeHaptics(this);
        }

        public virtual void ResetHaptics()
        {
            m_Rumble.ResetHaptics(this);
        }

        public virtual void SetMotorSpeeds(float lowFrequency, float highFrequency)
        {
            m_Rumble.SetMotorSpeeds(this, lowFrequency, highFrequency);
        }

        private DualMotorRumble m_Rumble;

        private static int s_GamepadCount;
        private static Gamepad[] s_Gamepads;
    }
}
