namespace ISX
{
    public static class CommonUsages
    {
        public static InternedString Primary2DMotion = new InternedString("Primary2DMotion");
        public static InternedString Secondary2DMotion = new InternedString("Secondary2DMotion");
        public static InternedString PrimaryAction = new InternedString("PrimaryAction");
        public static InternedString SecondaryAction = new InternedString("SecondaryAction");
        public static InternedString PrimaryTrigger = new InternedString("PrimaryTrigger");
        public static InternedString SecondaryTrigger = new InternedString("SecondaryTrigger");
        public static InternedString Back = new InternedString("Back");
        public static InternedString Forward = new InternedString("Forward");
        public static InternedString Menu = new InternedString("Menu");
        public static InternedString Submit = new InternedString("Submit");
        public static InternedString Cancel = new InternedString("Cancel");
        public static InternedString Previous = new InternedString("Previous");
        public static InternedString Next = new InternedString("Next");
        public static InternedString Modifier = new InternedString("Modifier"); // Stuff like CTRL
        public static InternedString ScrollHorizontal = new InternedString("ScrollHorizontal");
        public static InternedString Pressure = new InternedString("Pressure");
        public static InternedString Position = new InternedString("Position");
        public static InternedString Orientation = new InternedString("Orientation");

        // Rotation around single, fixed axis.
        // Example: twist on joystick or twist of pen (few pens support that).
        public static InternedString Twist = new InternedString("Twist");

        public static InternedString Point = new InternedString("Point");

        public static InternedString LowFreqMotor = new InternedString("LowFreqMotor");
        public static InternedString HighFreqMotor = new InternedString("HighFreqMotor");

        public static InternedString LeftHand = new InternedString("LeftHand");
        public static InternedString RightHand = new InternedString("RightHand");

        public static InternedString BatteryStrength = new InternedString("BatteryStrength");
    }
}
