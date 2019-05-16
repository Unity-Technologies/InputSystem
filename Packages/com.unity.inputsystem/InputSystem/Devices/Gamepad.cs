using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Haptics;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: come up with consistent naming for buttons; (xxxButton? xxx?)

////REVIEW: should we add a gyro as a standard feature of gamepads?

////REVIEW: is the Lefty layout variant actually useful?

////TODO: allow to be used for mouse simulation

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Default state layout for gamepads.
    /// </summary>
    /// <seealso cref="Gamepad"/>
    // NOTE: Must match GamepadInputState in native.
    [StructLayout(LayoutKind.Explicit, Size = 28)]
    public struct GamepadState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('G', 'P', 'A', 'D');

        /// <summary>
        /// Button bit mask.
        /// </summary>
        /// <seealso cref="GamepadButton"/>
        ////REVIEW: do we want the name to correspond to what's actually on the device?
        [InputControl(name = "dpad", layout = "Dpad", usage = "Hatswitch", displayName = "D-Pad")]
        [InputControl(name = "buttonSouth", layout = "Button", bit = (uint)GamepadButton.South, usages = new[] { "PrimaryAction", "Submit" }, aliases = new[] { "a", "cross" }, displayName = "Button South", shortDisplayName = "A")]
        [InputControl(name = "buttonWest", layout = "Button", bit = (uint)GamepadButton.West, usage = "SecondaryAction", aliases = new[] { "x", "square" }, displayName = "Button West", shortDisplayName = "X")]
        [InputControl(name = "buttonNorth", layout = "Button", bit = (uint)GamepadButton.North, aliases = new[] { "y", "triangle" }, displayName = "Button North", shortDisplayName = "Y")]
        [InputControl(name = "buttonEast", layout = "Button", bit = (uint)GamepadButton.East, usage = "Back", aliases = new[] { "b", "circle" }, displayName = "Button East", shortDisplayName = "B")]
        ////FIXME: 'Press' naming is inconsistent with 'Button' naming
        [InputControl(name = "leftStickPress", layout = "Button", bit = (uint)GamepadButton.LeftStick, displayName = "Left Stick Press")]
        [InputControl(name = "rightStickPress", layout = "Button", bit = (uint)GamepadButton.RightStick, displayName = "Right Stick Press")]
        [InputControl(name = "leftShoulder", layout = "Button", bit = (uint)GamepadButton.LeftShoulder, displayName = "Left Shoulder", shortDisplayName = "LB")]
        [InputControl(name = "rightShoulder", layout = "Button", bit = (uint)GamepadButton.RightShoulder, displayName = "Right Shoulder", shortDisplayName = "RB")]
        ////REVIEW: seems like these two should get less ambiguous names as well
        [InputControl(name = "start", layout = "Button", bit = (uint)GamepadButton.Start, usage = "Menu", displayName = "Start")]
        [InputControl(name = "select", layout = "Button", bit = (uint)GamepadButton.Select, displayName = "Select")]
        [FieldOffset(0)]
        public uint buttons;

        /// <summary>
        /// Left stick position.
        /// </summary>
        [InputControl(variants = "Default", layout = "Stick", usage = "Primary2DMotion", processors = "stickDeadzone", displayName = "Left Stick", shortDisplayName = "LS")]
        [InputControl(variants = "Lefty", layout = "Stick", usage = "Secondary2DMotion", processors = "stickDeadzone", displayName = "Left Stick", shortDisplayName = "LS")]
        [FieldOffset(4)]
        public Vector2 leftStick;

        /// <summary>
        /// Right stick position.
        /// </summary>
        [InputControl(variants = "Default", layout = "Stick", usage = "Secondary2DMotion", processors = "stickDeadzone", displayName = "Right Stick", shortDisplayName = "RS")]
        [InputControl(variants = "Lefty", layout = "Stick", usage = "Primary2DMotion", processors = "stickDeadzone", displayName = "Right Stick", shortDisplayName = "RS")]
        [FieldOffset(12)]
        public Vector2 rightStick;

        ////REVIEW: should left and right trigger get deadzones?

        /// <summary>
        /// Position of the left trigger.
        /// </summary>
        [InputControl(variants = "Default", layout = "Button", format = "FLT", usage = "SecondaryTrigger", displayName = "Left Trigger", shortDisplayName = "LT")]
        [InputControl(variants = "Lefty", layout = "Button", format = "FLT", usage = "PrimaryTrigger", displayName = "Left Trigger", shortDisplayName = "LT")]
        [FieldOffset(20)]
        public float leftTrigger;

        /// <summary>
        /// Position of the right trigger.
        /// </summary>
        [InputControl(variants = "Default", layout = "Button", format = "FLT", usage = "PrimaryTrigger", displayName = "Right Trigger", shortDisplayName = "RT")]
        [InputControl(variants = "Lefty", layout = "Button", format = "FLT", usage = "SecondaryTrigger", displayName = "Right Trigger", shortDisplayName = "RT")]
        [FieldOffset(24)]
        public float rightTrigger;

        public FourCC GetFormat()
        {
            return kFormat;
        }

        public GamepadState(params GamepadButton[] buttons)
            : this()
        {
            foreach (var button in buttons)
            {
                var bit = (uint)1 << (int)button;
                this.buttons |= bit;
            }
        }

        public GamepadState WithButton(GamepadButton button, bool value = true)
        {
            var bit = (uint)1 << (int)button;
            if (value)
                buttons |= bit;
            else
                buttons &= ~bit;
            return this;
        }
    }

    public enum GamepadButton
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
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// An Xbox-style gamepad with two sticks, a D-Pad, four face buttons, two triggers,
    /// two shoulder buttons, and two menu buttons.
    /// </summary>
    [InputControlLayout(stateType = typeof(GamepadState), isGenericTypeOfDevice = true)]
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

        /// <summary>
        /// Same as <see cref="buttonSouth"/>.
        /// </summary>
        public ButtonControl aButton
        {
            get { return buttonSouth; }
        }

        /// <summary>
        /// Same as <see cref="buttonEast"/>.
        /// </summary>
        public ButtonControl bButton
        {
            get { return buttonEast; }
        }

        /// <summary>
        /// Same as <see cref="buttonWest"/>
        /// </summary>
        public ButtonControl xButton
        {
            get { return buttonWest; }
        }

        /// <summary>
        /// Same as <see cref="buttonNorth"/>.
        /// </summary>
        public ButtonControl yButton
        {
            get { return buttonNorth; }
        }

        ////REVIEW: what about having 'axes' and 'buttons' read-only arrays like Joysticks and allowing to index that?
        public ButtonControl this[GamepadButton button]
        {
            get
            {
                switch (button)
                {
                    case GamepadButton.North: return buttonNorth;
                    case GamepadButton.South: return buttonSouth;
                    case GamepadButton.East: return buttonEast;
                    case GamepadButton.West: return buttonWest;
                    case GamepadButton.Start: return startButton;
                    case GamepadButton.Select: return selectButton;
                    case GamepadButton.LeftShoulder: return leftShoulder;
                    case GamepadButton.RightShoulder: return rightShoulder;
                    case GamepadButton.LeftStick: return leftStickButton;
                    case GamepadButton.RightStick: return rightStickButton;
                    case GamepadButton.DpadUp: return dpad.up;
                    case GamepadButton.DpadDown: return dpad.down;
                    case GamepadButton.DpadLeft: return dpad.left;
                    case GamepadButton.DpadRight: return dpad.right;
                    default:
                        throw new InvalidEnumArgumentException(nameof(button), (int)button, typeof(GamepadButton));
                }
            }
        }

        /// <summary>
        /// The gamepad last used by the user or null if there is no gamepad connected to the system.
        /// </summary>
        public static Gamepad current { get; private set; }

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
        public new static ReadOnlyArray<Gamepad> all => new ReadOnlyArray<Gamepad>(s_Gamepads, 0, s_GamepadCount);

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

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnAdded()
        {
            ArrayHelpers.AppendWithCapacity(ref s_Gamepads, ref s_GamepadCount, this);
        }

        protected override void OnRemoved()
        {
            if (current == this)
                current = null;

            // Remove from `all`.
            var wasFound = ArrayHelpers.Erase(ref s_Gamepads, this);
            Debug.Assert(wasFound, $"Gamepad {this} seems to not have been added but is being removed");
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
