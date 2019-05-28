using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    public static class CommonUsages
    {
        // Primary 2D motion control.
        // Example: left stick on gamepad.
        public static readonly InternedString Primary2DMotion = new InternedString("Primary2DMotion");

        // Secondary 2D motion control.
        // Example: right stick on gamepad.
        public static readonly InternedString Secondary2DMotion = new InternedString("Secondary2DMotion");

        public static readonly InternedString PrimaryAction = new InternedString("PrimaryAction");
        public static readonly InternedString SecondaryAction = new InternedString("SecondaryAction");
        public static readonly InternedString PrimaryTrigger = new InternedString("PrimaryTrigger");
        public static readonly InternedString SecondaryTrigger = new InternedString("SecondaryTrigger");
        public static readonly InternedString Modifier = new InternedString("Modifier"); // Stuff like CTRL
        public static readonly InternedString Position = new InternedString("Position");
        public static readonly InternedString Orientation = new InternedString("Orientation");
        public static readonly InternedString Hatswitch = new InternedString("Hatswitch");

        // Button to navigate to previous location.
        // Example: Escape on keyboard, B button on gamepad.
        public static readonly InternedString Back = new InternedString("Back");

        // Button to navigate to next location.
        public static readonly InternedString Forward = new InternedString("Forward");

        // Button to bring up menu.
        public static readonly InternedString Menu = new InternedString("Menu");

        // Button to confirm the current choice.
        public static readonly InternedString Accept = new InternedString("Accept");

        ////REVIEW: isn't this the same as "Back"?
        // Button to not accept the current choice.
        public static readonly InternedString Cancel = new InternedString("Cancel");

        // Horizontal motion axis.
        // Example: X axis on mouse.
        public static readonly InternedString Horizontal = new InternedString("Horizontal");

        // Vertical motion axis.
        // Example: Y axis on mouse.
        public static readonly InternedString Vertical = new InternedString("Vertical");

        // Rotation around single, fixed axis.
        // Example: twist on joystick or twist of pen (few pens support that).
        public static readonly InternedString Twist = new InternedString("Twist");

        // Pressure level axis.
        // Example: pen pressure.
        public static readonly InternedString Pressure = new InternedString("Pressure");

        // Axis to scroll horizontally.
        public static readonly InternedString ScrollHorizontal = new InternedString("ScrollHorizontal");

        // Axis to scroll vertically.
        public static readonly InternedString ScrollVertical = new InternedString("ScrollVertical");

        public static readonly InternedString Point = new InternedString("Point");

        public static readonly InternedString LowFreqMotor = new InternedString("LowFreqMotor");
        public static readonly InternedString HighFreqMotor = new InternedString("HighFreqMotor");

        // Device in left hand.
        // Example: left hand XR controller.
        public static readonly InternedString LeftHand = new InternedString("LeftHand");

        // Device in right hand.
        // Example: right hand XR controller.
        public static readonly InternedString RightHand = new InternedString("RightHand");

        // Axis representing charge of battery (1=full, 0=empty).
        public static readonly InternedString BatteryStrength = new InternedString("BatteryStrength");
    }
}
