using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    public static class CommonUsages
    {
        // Primary 2D motion control.
        // Example: left stick on gamepad.
        public static InternedString Primary2DMotion = new InternedString("Primary2DMotion");

        // Secondary 2D motion control.
        // Example: right stick on gamepad.
        public static InternedString Secondary2DMotion = new InternedString("Secondary2DMotion");

        public static InternedString PrimaryAction = new InternedString("PrimaryAction");
        public static InternedString SecondaryAction = new InternedString("SecondaryAction");
        public static InternedString PrimaryTrigger = new InternedString("PrimaryTrigger");
        public static InternedString SecondaryTrigger = new InternedString("SecondaryTrigger");
        public static InternedString Modifier = new InternedString("Modifier"); // Stuff like CTRL
        public static InternedString Position = new InternedString("Position");
        public static InternedString Orientation = new InternedString("Orientation");
        public static InternedString Hatswitch = new InternedString("Hatswitch");

        // Button to navigate to previous location.
        // Example: Escape on keyboard, B button on gamepad.
        public static InternedString Back = new InternedString("Back");

        // Button to navigate to next location.
        public static InternedString Forward = new InternedString("Forward");

        // Button to bring up menu.
        public static InternedString Menu = new InternedString("Menu");

        // Button to confirm the current choice.
        public static InternedString Accept = new InternedString("Accept");

        ////REVIEW: isn't this the same as "Back"?
        // Button to not accept the current choice.
        public static InternedString Cancel = new InternedString("Cancel");

        // Horizontal motion axis.
        // Example: X axis on mouse.
        public static InternedString Horizontal = new InternedString("Horizontal");

        // Vertical motion axis.
        // Example: Y axis on mouse.
        public static InternedString Vertical = new InternedString("Vertical");

        // Rotation around single, fixed axis.
        // Example: twist on joystick or twist of pen (few pens support that).
        public static InternedString Twist = new InternedString("Twist");

        // Pressure level axis.
        // Example: pen pressure.
        public static InternedString Pressure = new InternedString("Pressure");

        // Axis to scroll horizontally.
        public static InternedString ScrollHorizontal = new InternedString("ScrollHorizontal");

        // Axis to scroll vertically.
        public static InternedString ScrollVertical = new InternedString("ScrollVertical");

        public static InternedString Point = new InternedString("Point");

        public static InternedString LowFreqMotor = new InternedString("LowFreqMotor");
        public static InternedString HighFreqMotor = new InternedString("HighFreqMotor");

        // Device in left hand.
        // Example: left hand XR controller.
        public static InternedString LeftHand = new InternedString("LeftHand");

        // Device in right hand.
        // Example: right hand XR controller.
        public static InternedString RightHand = new InternedString("RightHand");

        // Axis representing charge of battery (1=full, 0=empty).
        public static InternedString BatteryStrength = new InternedString("BatteryStrength");
    }
}
