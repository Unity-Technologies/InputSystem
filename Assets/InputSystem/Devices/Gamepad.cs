using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    // Xbox-compatible gamepad state layout.
    // Must be kept identical to layout used by native code.
    // Native will send StateEvents with data matching this struct to update gamepads.
    [StructLayout(LayoutKind.Sequential)]
    public struct GamepadState : IInputStateTypeInfo
    {
        public static FourCC kStateTypeCode
        {
            get { return new FourCC('G', 'P', 'A', 'D'); }
        }

        [InputControl(name = "dpad", template = "Dpad")]
        [InputControl(name = "buttonSouth", template = "Button", bit = (int)Button.South, usage = "PrimaryAction", aliases = new[] { "a", "cross" })]
        [InputControl(name = "buttonWest", template = "Button", bit = (int)Button.West, usage = "SecondaryAction")]
        [InputControl(name = "buttonNorth", template = "Button", bit = (int)Button.North)]
        [InputControl(name = "buttonEast", template = "Button", bit = (int)Button.East)]
        [InputControl(name = "leftStickPress", template = "Button", bit = (int)Button.LeftStick)]
        [InputControl(name = "rightStickPress", template = "Button", bit = (int)Button.RightStick)]
        [InputControl(name = "leftShoulder", template = "Button", bit = (int)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", template = "Button", bit = (int)Button.RightShoulder)]
        ////REVIEW: seems like these two should get less ambiguous names as well
        [InputControl(name = "start", template = "Button", bit = (int)Button.Start, usage = "Menu")]
        [InputControl(name = "select", template = "Button", bit = (int)Button.Select)]
        public int buttons;

        [InputControl(template = "Stick", usage = "PrimaryStick")]
        public Vector2 leftStick;
        [InputControl(template = "Stick", usage = "SecondaryStick")]
        public Vector2 rightStick;
        [InputControl(template = "Analog", usage = "SecondaryTrigger")]
        public float leftTrigger;
        [InputControl(template = "Analog", usage = "PrimaryTrigger")]
        public float rightTrigger;

        public GamepadOutputState motors;

        public enum Button
        {
            // Dpad buttons. Important to be first in the bitfield as we'll
            // point the DpadControl to it.
            // IMPORTANT: Order has to match what is expected by DpadControl.
            DpadUp,
            DpadDown,
            DpadRight,
            DpadLeft,

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
            B = West,

            // Aliases PS4 style.
            Cross = South,
            Square = West,
            Triangle = North,
            Circle = South,
        }

        public FourCC GetTypeStatic()
        {
            return kStateTypeCode;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GamepadOutputState : IInputStateTypeInfo
    {
        public static FourCC kStateTypeCode
        {
            get { return new FourCC('M', 'O', 'T', 'R'); }
        }

        [InputControl(name = "leftMotor", template = "Motor", usage = "LowFreqMotor")]
        public float leftMotor;
        [InputControl(name = "rightMotor", template = "Motor", usage = "HighFreqMotor")]
        public float rightMotor;

        public FourCC GetTypeStatic()
        {
            return kStateTypeCode;
        }
    }

    [InputState(typeof(GamepadState))]
    public class Gamepad : InputDevice
    {
        public GamepadState state
        {
            get
            {
                unsafe
                {
                    return *((GamepadState*)currentValuePtr);
                }
            }
        }

        public override object valueAsObject
        {
            get { return state; }
        }

        // Given that the north/east/south/west directions are awkward to use,
        // we expose the controls using Xbox style naming. However, we still look
        // them up using directions so the underlying controls should be the right
        // ones.
        public ButtonControl x { get; private set; }
        public ButtonControl y { get; private set; }
        public ButtonControl a { get; private set; }
        public ButtonControl b { get; private set; }

        public ButtonControl leftStickPress { get; private set; }
        public ButtonControl rightStickPress { get; private set; }

        public ButtonControl start { get; private set; }
        public ButtonControl select { get; private set; }

        public DpadControl dpad { get; private set; }

        public ButtonControl leftShoulder { get; private set; }
        public ButtonControl rightShoulder { get; private set; }

        public StickControl leftStick { get; private set; }
        public StickControl rightStick { get; private set; }

        public AxisControl leftTrigger { get; private set; }
        public AxisControl rightTrigger { get; private set; }

        public AxisControl leftMotor { get; private set; }
        public AxisControl rightMotor { get; private set; }

        public static Gamepad current { get; protected set; }

        public Gamepad()
        {
            m_StateBlock.typeCode = GamepadState.kStateTypeCode;
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            x = setup.GetControl<ButtonControl>(this, "buttonWest");
            y = setup.GetControl<ButtonControl>(this, "buttonNorth");
            a = setup.GetControl<ButtonControl>(this, "buttonSouth");
            b = setup.GetControl<ButtonControl>(this, "buttonEast");

            start = setup.GetControl<ButtonControl>(this, "start");
            select = setup.GetControl<ButtonControl>(this, "select");

            leftStickPress = setup.GetControl<ButtonControl>(this, "leftStickPress");
            rightStickPress = setup.GetControl<ButtonControl>(this, "rightStickPress");

            dpad = setup.GetControl<DpadControl>(this, "dpad");

            leftShoulder = setup.GetControl<ButtonControl>(this, "leftShoulder");
            rightShoulder = setup.GetControl<ButtonControl>(this, "rightShoulder");

            leftStick = setup.GetControl<StickControl>(this, "leftStick");
            rightStick = setup.GetControl<StickControl>(this, "rightStick");

            leftTrigger = setup.GetControl<AxisControl>(this, "leftTrigger");
            rightTrigger = setup.GetControl<AxisControl>(this, "rightTrigger");

            leftMotor = setup.GetControl<AxisControl>(this, "leftMotor");
            rightMotor = setup.GetControl<AxisControl>(this, "rightMotor");

            base.FinishSetup(setup);
        }
    }
}
