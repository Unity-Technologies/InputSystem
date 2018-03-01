using System;
using ISX.Processors;

namespace ISX
{
    /// <summary>
    /// Static configuration values that are picked up by the various pieces of logic.
    /// </summary>
    /// <remarks>
    /// The values are not const and can be changed on the fly.
    /// </remarks>
    public static class InputConfiguration
    {
        /// <summary>
        /// Default value used when nothing is set explicitly on <see cref="DeadzoneProcessor.min"/>.
        /// </summary>
        public static float DeadzoneMin = 0.125f;

        // Default value used when nothing is set explicitly on DeadzoneProcessor.max.
        public static float DeadzoneMax = 0.925f;

        // If a button is stored as anything but a bit, this is the threshold the value
        // of the button has to cross in order for the button to be considered pressed.
        public static float ButtonPressPoint = 0.15f;

        // Before how long does a button have to be released for it to be considered a click?
        public static float TapTime = 0.2f;

        // After how long do does a button have to be held and then released for it to be considered a "slow click"?
        public static float SlowTapTime = 0.5f;

        // After how long a time do we no longer consider clicks consecutive?
        public static float MultiTapMaximumDelay = 0.75f;

        // How long does a button have to be held for it to be considered a hold?
        public static float HoldTime = 0.4f;

        #if UNITY_EDITOR
        // We support input in edit mode as well. If the editor is in play mode and the game view
        // has focus, input goes to the game. Otherwise input goes to the editor. This behavior can
        // be annoying so this switch allows to route input exclusively to the game.
        public static bool LockInputToGame;
        #endif

        [Serializable]
        internal struct SerializedState
        {
            public float deadzoneMin;
            public float deadzoneMax;
            public float buttonPressPoint;
            public float tapTime;
            public float slowTapTime;
            public float multiTapMaximumDelay;
            public float holdTime;
            #if UNITY_EDITOR
            public bool lockInputToGame;
            #endif
        }

        internal static SerializedState Save()
        {
            return new SerializedState
            {
                deadzoneMin = DeadzoneMin,
                deadzoneMax = DeadzoneMax,
                buttonPressPoint = ButtonPressPoint,
                tapTime = TapTime,
                slowTapTime = SlowTapTime,
                multiTapMaximumDelay = MultiTapMaximumDelay,
                holdTime = HoldTime,
                #if UNITY_EDITOR
                lockInputToGame = LockInputToGame
                #endif
            };
        }

        internal static void Restore(SerializedState state)
        {
            DeadzoneMin = state.deadzoneMin;
            DeadzoneMax = state.deadzoneMax;
            TapTime = state.tapTime;
            SlowTapTime = state.slowTapTime;
            MultiTapMaximumDelay = state.multiTapMaximumDelay;
            HoldTime = state.holdTime;
            #if UNITY_EDITOR
            LockInputToGame = state.lockInputToGame;
            #endif
        }
    }
}
