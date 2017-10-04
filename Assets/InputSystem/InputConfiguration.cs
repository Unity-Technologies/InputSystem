namespace ISX
{
    // Static configuration values that are picked up by the various pieces of logic.
    // These are not const and can be changed by the user.
    public static class InputConfiguration
    {
        // Before how long does a button have to be released for it to be considered a click?
        public static float ClickTime = 0.2f;

        // After how long do does a button have to be held and then released for it to be considered a "slow click"?
        public static float SlowClickTime = 0.5f;

        // After how long a time do we no longer consider clicks consecutive?
        public static float MultiClickMaximumDelay = 0.75f;

        // How long does a button have to be held for it to be considered a hold?
        public static float HoldTime = 0.4f;
    }
}
