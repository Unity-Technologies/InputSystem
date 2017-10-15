namespace ISX
{
    // Static configuration values that are picked up by the various pieces of logic.
    // These are not const and can be changed by the user.
    public static class InputConfiguration
    {
        // Default value used when nothing is set explicitly on DeadzoneProcessor.min.
        public static float DefaultDeadzoneMin = 0.125f;

        // Default value used when nothing is set explicitly on DeadzoneProcessor.max.
        public static float DefaultDeadzoneMax = 0.925f;

        // If a button is stored as anything but a bit, this is the threshold the value
        // of the button has to cross in order for the button to be considered pressed.
        public static float ButtonPressPoint = 0.5f;

        // Before how long does a button have to be released for it to be considered a click?
        public static float TapTime = 0.2f;

        // After how long do does a button have to be held and then released for it to be considered a "slow click"?
        public static float SlowTapTime = 0.5f;

        // After how long a time do we no longer consider clicks consecutive?
        public static float MultiTapMaximumDelay = 0.75f;

        // How long does a button have to be held for it to be considered a hold?
        public static float HoldTime = 0.4f;

        internal struct SerializedState
        {
            public float defaultDeadzoneMin;
            public float defaultDeadzoneMax;
            public float buttonPressPoint;
            public float tapTime;
            public float slowTapTime;
            public float multiTapMaximumDelay;
            public float holdTime;
        }

        internal static SerializedState Save()
        {
            return new SerializedState
            {
                defaultDeadzoneMin = DefaultDeadzoneMin,
                defaultDeadzoneMax = DefaultDeadzoneMax,
                buttonPressPoint = ButtonPressPoint,
                tapTime = TapTime,
                slowTapTime = SlowTapTime,
                multiTapMaximumDelay = MultiTapMaximumDelay,
                holdTime = HoldTime
            };
        }

        internal static void Restore(SerializedState state)
        {
            DefaultDeadzoneMin = state.defaultDeadzoneMin;
            DefaultDeadzoneMax = state.defaultDeadzoneMax;
            TapTime = state.tapTime;
            SlowTapTime = state.slowTapTime;
            MultiTapMaximumDelay = state.multiTapMaximumDelay;
            HoldTime = state.holdTime;
        }
    }
}
