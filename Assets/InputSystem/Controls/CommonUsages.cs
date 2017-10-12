namespace ISX
{
    public static class CommonUsages
    {
        ////REVIEW: may want to rename to PrimaryMotion and SecondaryMotion or something like that
        public const string PrimaryStick = "PrimaryStick";
        public const string SecondaryStick = "SecondaryStick";
        public const string PrimaryAction = "PrimaryAction";
        public const string SecondaryAction = "SecondaryAction";
        public const string PrimaryTrigger = "PrimaryTrigger";
        public const string SecondaryTrigger = "SecondaryTrigger";
        public const string Back = "Back";
        public const string Forward = "Forward";
        public const string Menu = "Menu";
        public const string Submit = "Submit";
        public const string Cancel = "Cancel";
        public const string Previous = "Previous";
        public const string Next = "Next";
        public const string Modifier = "Modifier"; // Stuff like CTRL
        public const string ScrollHorizontal = "ScrollHorizontal";
        public const string Pressure = "Pressure";
        public const string Position = "Position";
        public const string Orientation = "Orientation";

        // Rotation around single, fixed axis.
        // Example: twist on joystick or twist of pen (few pens support that).
        public const string Twist = "Twist";

        public const string Point = "Point";

        public const string LowFreqMotor = "LowFreqMotor";
        public const string HighFreqMotor = "HighFreqMotor";

        public const string LeftHand = "LeftHand";
        public const string RightHand = "RightHand";
    }
}
