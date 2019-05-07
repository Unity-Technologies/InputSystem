using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    public static class CommonUsages
    {
        static CommonUsages()
        {
            Accept = new InternedString("Accept");
            Back = new InternedString("Back");
            BatteryStrength = new InternedString("BatteryStrength");
            Cancel = new InternedString("Cancel");
            Forward = new InternedString("Forward");
            Hatswitch = new InternedString("Hatswitch");
            HighFreqMotor = new InternedString("HighFreqMotor");
            Horizontal = new InternedString("Horizontal");
            LeftHand = new InternedString("LeftHand");
            LowFreqMotor = new InternedString("LowFreqMotor");
            Menu = new InternedString("Menu");
            Modifier = new InternedString("Modifier"); 
            Orientation = new InternedString("Orientation");
            Point = new InternedString("Point");
            Position = new InternedString("Position");
            Pressure = new InternedString("Pressure");
            Primary2DMotion = new InternedString("Primary2DMotion");
            PrimaryAction = new InternedString("PrimaryAction");
            PrimaryTrigger = new InternedString("PrimaryTrigger");
            RightHand = new InternedString("RightHand");
            ScrollHorizontal = new InternedString("ScrollHorizontal");
            ScrollVertical = new InternedString("ScrollVertical");
            Secondary2DMotion = new InternedString("Secondary2DMotion");
            SecondaryAction = new InternedString("SecondaryAction");
            SecondaryTrigger = new InternedString("SecondaryTrigger");
            Twist = new InternedString("Twist");
            Vertical = new InternedString("Vertical");
        }
        // Primary 2D motion control.
        // Example: left stick on gamepad.
        public static InternedString Primary2DMotion { get; }

        // Secondary 2D motion control.
        // Example: right stick on gamepad.
        public static InternedString Secondary2DMotion { get; }

        public static InternedString PrimaryAction { get; }
        public static InternedString SecondaryAction { get; }
        public static InternedString PrimaryTrigger { get; }
        public static InternedString SecondaryTrigger { get; }
        public static InternedString Modifier { get; } // Stuff like CTRL
        public static InternedString Position { get; }
        public static InternedString Orientation { get; }
        public static InternedString Hatswitch { get; }

        // Button to navigate to previous location.
        // Example: Escape on keyboard, B button on gamepad.
        public static InternedString Back { get; }

        // Button to navigate to next location.
        public static InternedString Forward { get; }

        // Button to bring up menu.
        public static InternedString Menu { get; }

        // Button to confirm the current choice.
        public static InternedString Accept { get; }

        ////REVIEW: isn't this the same as "Back"?
        // Button to not accept the current choice.
        public static InternedString Cancel { get; }

        // Horizontal motion axis.
        // Example: X axis on mouse.
        public static InternedString Horizontal { get; }

        // Vertical motion axis.
        // Example: Y axis on mouse.
        public static InternedString Vertical { get; }

        // Rotation around single, fixed axis.
        // Example: twist on joystick or twist of pen (few pens support that).
        public static InternedString Twist { get; }

        // Pressure level axis.
        // Example: pen pressure.
        public static InternedString Pressure { get; }

        // Axis to scroll horizontally.
        public static InternedString ScrollHorizontal { get; }

        // Axis to scroll vertically.
        public static InternedString ScrollVertical { get; }

        public static InternedString Point { get; }

        public static InternedString LowFreqMotor { get; }
        public static InternedString HighFreqMotor { get; }

        // Device in left hand.
        // Example: left hand XR controller.
        public static InternedString LeftHand { get; }

        // Device in right hand.
        // Example: right hand XR controller.
        public static InternedString RightHand { get; }

        // Axis representing charge of battery (1=full, 0=empty).
        public static InternedString BatteryStrength { get; }
    }
}
