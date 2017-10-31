using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    ////REVIEW: I think it makes sense to switch this to a more compact format that doesn't store floats; after all in almost all
    ////        cases our source data on platforms is *not* floats. And users won't generally deal with GamepadState directly.

    // Default gamepad state layout.
    [StructLayout(LayoutKind.Sequential)]
    public struct GamepadState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('G', 'P', 'A', 'D');

        ////REVIEW: do we want the name to correspond to what's actually on the device?
        [InputControl(name = "dpad", template = "Dpad")]
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
        public uint buttons;

        [InputControl(variant = "Default", template = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        [InputControl(variant = "Lefty", template = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        public Vector2 leftStick;

        [InputControl(variant = "Default", template = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        [InputControl(variant = "Lefty", template = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        public Vector2 rightStick;

        ////REVIEW: shouldn't this be an axis? how do we make sure actions trigger only on crossing threshold?
        [InputControl(variant = "Default", template = "Button", format = "FLT", usage = "SecondaryTrigger")]
        [InputControl(variant = "Lefty", template = "Button", format = "FLT", usage = "PrimaryTrigger")]
        public float leftTrigger;

        [InputControl(variant = "Default", template = "Button", format = "FLT", usage = "PrimaryTrigger")]
        [InputControl(variant = "Lefty", template = "Button", format = "FLT", usage = "SecondaryTrigger")]
        public float rightTrigger;

        public GamepadOutputState motors;

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

    [StructLayout(LayoutKind.Sequential)]
    public struct GamepadOutputState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('M', 'O', 'T', 'R');

        [InputControl(name = "leftMotor", template = "Motor", usage = "LowFreqMotor")]
        public float leftMotor;
        [InputControl(name = "rightMotor", template = "Motor", usage = "HighFreqMotor")]
        public float rightMotor;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [InputState(typeof(GamepadState))]
    public class Gamepad : InputDevice
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

        public AxisControl leftMotor { get; private set; }
        public AxisControl rightMotor { get; private set; }

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

            leftMotor = setup.GetControl<AxisControl>(this, "leftMotor");
            rightMotor = setup.GetControl<AxisControl>(this, "rightMotor");

            base.FinishSetup(setup);
        }
    }
}
