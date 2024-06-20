#if UNITY_EDITOR || UNITY_ANDROID || PACKAGE_DOCS_GENERATION
using System;

namespace UnityEngine.InputSystem
{
    public partial class InputSettings
    {
        /// <summary>
        /// Project-wide input settings for the Android platform.
        /// </summary>
        [Serializable]
        public class AndroidSettings
        {
            /// <summary>
            /// Determines whether pressing the back button on the device should leave the app or not.
            /// </summary>
            /// <remarks>
            /// On Android 13 and above, you can use this option in combination with `PlayerSettings.Android.predictiveBackSupport`
            /// to enable predictive back animations.
            ///
            /// By default this property is set to false, which means you're responsible for responding to Back button.
            /// </remarks>
            /// <code>
            /// if (!InputSystem.settings.android.backButtonLeavesApp && Keyboard.current.escapeKey.wasReleasedThisFrame)
            /// {
            ///     // Handle back button press
            /// }
            /// </code>
            public bool backButtonLeavesApp
            {
                get
                {
                    return m_BackButtonLeavesApp;
                }
                set
                {
                    if (value == m_BackButtonLeavesApp)
                        return;
                    m_BackButtonLeavesApp = value;
                    InputSystem.s_Manager.ApplySettings();
                }
            }

            [SerializeField] private bool m_BackButtonLeavesApp = false;
        }

        /// <summary>
        /// Android-specific settings.
        /// </summary>
        /// <remarks>
        /// This is only accessible in the editor or Android players.
        /// </remarks>
        public AndroidSettings android => m_AndroidSettings;

        [SerializeField]
        private AndroidSettings m_AndroidSettings = new AndroidSettings();
    }
}
#endif
