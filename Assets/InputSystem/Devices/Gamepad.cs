using System.Runtime.InteropServices;
using UnityEngine;

////REVIEW: is there still the need to have separate state structs or can the unification of devices
////        and controls obsolete that need?

namespace ISX
{
    // Xbox-compatible gamepad state layout.
    // Must be kept identical to layout used by native code.
    // This struct is one example of how to yield InputControlSetups; however, the system doesn't
    // care how layouts/setups come to be.
    [StructLayout(LayoutKind.Sequential)]
    public struct GamepadState : IInputStateTypeInfo
    {
        public static FourCC kStateTypeCode
        {
            get { return new FourCC('G', 'P', 'A', 'D'); }
        }

	    [InputControl(name = "dpad", type = "Dpad")]
        [InputControl(name = "buttonSouth", type = "Button", bit = (int)Button.South, usage = "PrimaryAction", aliases = new[] { "a", "cross" })]
        [InputControl(name = "buttonWest", type = "Button", bit = (int)Button.West, usage = "SecondaryAction")]
        [InputControl(name = "buttonNorth", type = "Button", bit = (int)Button.North)]
        [InputControl(name = "buttonEast", type = "Button", bit = (int)Button.East)]
        [InputControl(name = "leftStickPress", type = "Button", usage = "primaryStick", bit = (int)Button.LeftStick)]
        [InputControl(name = "rightStickPress", type = "Button", usage = "secondaryStick", bit = (int)Button.RightStick)]
        public int buttons;

        [InputControl(type = "Stick", usage = "PrimaryStick")]
        public Vector2 leftStick;
        [InputControl(type = "Stick", usage = "SecondaryStick")]
        public Vector2 rightStick;
        [InputControl(type = "Analog", usage = "SecondaryTrigger")]
        public float leftTrigger;
        [InputControl(type = "Analog", usage = "PrimaryTrigger")]
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
            LeftBumper,
            RightBumper,

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
		
		[InputControl(name = "left", type = "Motor", usage = "LowFreqMotor")]
		public float leftMotorSpeed;
		[InputControl(name = "right", type = "Motor", usage = "HighFreqMotor")]
		public float rightMotorSpeed;

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
	                return *((GamepadState*) currentStatePtr);
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

        public ButtonControl leftShoulder { get; private set; }
        public ButtonControl rightShoulder { get; private set; }

        public StickControl leftStick { get; private set; }
        public StickControl rightStick { get; private set; }
		
		public AxisControl leftMotor { get; private set; }
		public AxisControl rightMotor { get; private set; }

		public static Gamepad current { get; protected set; }

		public Gamepad()
		{
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

			leftShoulder = setup.GetControl<ButtonControl>(this, "leftShoulder");
			rightShoulder = setup.GetControl<ButtonControl>(this, "rightShoulder");

			leftStick = setup.GetControl<StickControl>(this, "leftStick");
			rightStick = setup.GetControl<StickControl>(this, "rightStick");

			leftMotor = setup.GetControl<AxisControl>(this, "leftMotor");
			rightMotor = setup.GetControl<AxisControl>(this, "rightMotor");

			base.FinishSetup(setup);
		}
	}
}