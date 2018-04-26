using System;
using UnityEngine.Experimental.Input.Processors;

namespace UnityEngine.Experimental.Input
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

        ////TODO: add support for disabling pointer sensitivity globally

        /// <summary>
        /// Default sensitivity for pointer deltas.
        /// </summary>
        /// <remarks>
        /// Pointer deltas flow into the system in pixel space. This means that the values are dependent on
        /// resolution and are relatively large. Whereas a gamepad thumbstick axis will have normalized
        /// values between [0..1], a pointer delta will easily be in the range of tens or even hundreds of pixels.
        ///
        /// Pointer sensitivity scaling allows to turn these pointer deltas into useful, partially resolution-independent
        /// floating-point values.
        ///
        /// The value determines how much travel is generated on the delta for each percent of travel across the
        /// window space. If, for example, the mouse moves 15 pixels on the X axis and -20 pixels on the Y axis,
        /// and if the player window is 640x480 pixels and the sensitivity setting is 0.5, then the generated
        /// pointer delta value will be (15/640*6, -20/480*6) = (0.14, -0.25).
        /// </remarks>
        /// <seealso cref="Pointer.delta"/>
        public static float PointerDeltaSensitivity = 0.25f;

        /// <summary>
        /// Should sensors be compensated for screen orientation.
        /// Compensated sensors are accelerometer, compass, gyroscope.
        /// </summary>
        public static bool CompensateSensorsForScreenOrientation = true;

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
            public float pointerDeltaSensitivity;
            public bool compensateSensorsForScreenOrientation;
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
                pointerDeltaSensitivity = PointerDeltaSensitivity,
                compensateSensorsForScreenOrientation = CompensateSensorsForScreenOrientation,
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
            PointerDeltaSensitivity = state.pointerDeltaSensitivity;
            CompensateSensorsForScreenOrientation = state.compensateSensorsForScreenOrientation;
            #if UNITY_EDITOR
            LockInputToGame = state.lockInputToGame;
            #endif
        }
    }
}
