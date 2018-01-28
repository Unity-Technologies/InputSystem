using System;
using System.Runtime.InteropServices;
using ISX.Haptics;
using ISX.Utilities;
using UnityEngine;

// use case: audio on GP (ps4 mic)
// use case: player ID and change on same GP

namespace ISX
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
        [InputControl(name = "dpad", template = "Dpad", usage = "Hatswitch")]
        [InputControl(name = "buttonSouth", template = "Button", bit = (uint)Button.South, usage = "PrimaryAction", aliases = new[] { "a", "cross" })]
        [InputControl(name = "buttonWest", template = "Button", bit = (uint)Button.West, usage = "SecondaryAction", aliases = new[] { "x", "square" })]
        [InputControl(name = "buttonNorth", template = "Button", bit = (uint)Button.North, aliases = new[] { "y", "triangle" })]
        [InputControl(name = "buttonEast", template = "Button", bit = (uint)Button.East, usage = "Back", aliases = new[] { "b", "circle" })]
        [InputControl(name = "leftStickPress", template = "Button", bit = (uint)Button.LeftStick)]
        [InputControl(name = "rightStickPress", template = "Button", bit = (uint)Button.RightStick)]
        [InputControl(name = "leftShoulder", template = "Button", bit = (uint)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", template = "Button", bit = (uint)Button.RightShoulder)]
        ////REVIEW: seems like these two should get less ambiguous names as well
        [InputControl(name = "start", template = "Button", bit = (uint)Button.Start, usage = "Menu")]
        [InputControl(name = "select", template = "Button", bit = (uint)Button.Select)]
        [FieldOffset(0)]
        public uint buttons;

        /// <summary>
        /// Left stick position.
        /// </summary>
        [InputControl(variant = "Default", template = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        [InputControl(variant = "Lefty", template = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        [FieldOffset(4)]
        public Vector2 leftStick;

        /// <summary>
        /// Right stick position.
        /// </summary>
        [InputControl(variant = "Default", template = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        [InputControl(variant = "Lefty", template = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        [FieldOffset(12)]
        public Vector2 rightStick;

        /// <summary>
        /// Position of the left trigger.
        /// </summary>
        [InputControl(variant = "Default", template = "Button", format = "FLT", usage = "SecondaryTrigger")]
        [InputControl(variant = "Lefty", template = "Button", format = "FLT", usage = "PrimaryTrigger")]
        [FieldOffset(20)]
        public float leftTrigger;

        /// <summary>
        /// Position of the right trigger.
        /// </summary>
        [InputControl(variant = "Default", template = "Button", format = "FLT", usage = "PrimaryTrigger")]
        [InputControl(variant = "Lefty", template = "Button", format = "FLT", usage = "SecondaryTrigger")]
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
    }

    /// <summary>
    /// An Xbox-style gamepad with two switcks, a D-Pad, four face buttons, two triggers,
    /// two shoulder buttons, and two menu buttons.
    /// </summary>
    /// <seealso cref="GamepadState"/>
    [InputTemplate(stateType = typeof(GamepadState))]
    public class Gamepad : InputDevice, IDualMotorRumble
    {
        // Given that the north/east/south/west directions are awkward to use,
        // we expose the controls using Xbox style naming. However, we still look
        // them up using directions so the underlying controls should be the right
        // ones.
        public ButtonControl xButton { get; private set; }
        public ButtonControl yButton { get; private set; }
        public ButtonControl aButton { get; private set; }
        public ButtonControl bButton { get; private set; }

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

        ////TODO: we need to split gamepad input and output state such that events can send state without including output

        public static Gamepad current { get; internal set; }

        public static ReadOnlyArray<Gamepad> all
        {
            get { throw new NotImplementedException(); }
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            xButton = setup.GetControl<ButtonControl>(this, "buttonWest");
            yButton = setup.GetControl<ButtonControl>(this, "buttonNorth");
            aButton = setup.GetControl<ButtonControl>(this, "buttonSouth");
            bButton = setup.GetControl<ButtonControl>(this, "buttonEast");

            startButton = setup.GetControl<ButtonControl>(this, "start");
            selectButton = setup.GetControl<ButtonControl>(this, "select");

            leftStickButton = setup.GetControl<ButtonControl>(this, "leftStickPress");
            rightStickButton = setup.GetControl<ButtonControl>(this, "rightStickPress");

            dpad = setup.GetControl<DpadControl>(this, "dpad");

            leftShoulder = setup.GetControl<ButtonControl>(this, "leftShoulder");
            rightShoulder = setup.GetControl<ButtonControl>(this, "rightShoulder");

            leftStick = setup.GetControl<StickControl>(this, "leftStick");
            rightStick = setup.GetControl<StickControl>(this, "rightStick");

            leftTrigger = setup.GetControl<ButtonControl>(this, "leftTrigger");
            rightTrigger = setup.GetControl<ButtonControl>(this, "rightTrigger");

            base.FinishSetup(setup);
        }

        public void PauseHaptics()
        {
            m_Rumble.PauseHaptics(this);
        }

        public void ResumeHaptics()
        {
            m_Rumble.ResumeHaptics(this);
        }

        public void ResetHaptics()
        {
            m_Rumble.ResetHaptics(this);
        }

        public void SetMotorSpeeds(float lowFrequency, float highFrequency)
        {
            m_Rumble.SetMotorSpeeds(this, lowFrequency, highFrequency);
        }

        private DualMotorRumble m_Rumble;
    }
}
