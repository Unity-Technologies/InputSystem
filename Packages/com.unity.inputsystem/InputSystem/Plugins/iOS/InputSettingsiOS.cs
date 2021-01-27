#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS || PACKAGE_DOCS_GENERATION
using System;
using UnityEngine.InputSystem.iOS;

namespace UnityEngine.InputSystem.iOS
{
    /// <summary>
    /// Governs access to a privacy-related resource on the user's device. Corresponds to a key in the application's
    /// Information Property List (Info.plist).
    /// </summary>
    /// <seealso href="https://developer.apple.com/documentation/bundleresources/information_property_list/protected_resources"/>
    [Serializable]
    public class PrivacyDataUsage
    {
        /// <summary>
        /// Whether access to the respective resource will be requested.
        /// </summary>
        /// <remarks>
        /// Before accessing a resource or a sensor, you need to explicitly enable the usage for it, otherwise the access for the resource will be denied.
        ///
        /// If this is set to true, the respective protected resource key will be entered in the application's Information Property List (Info.plist)
        /// using <see cref="usageDescription"/>.
        /// </remarks>
        public bool enabled
        {
            get => m_Enabled;
            set => m_Enabled = value;
        }

        /// <summary>
        /// Provide meaningful usage description.
        /// </summary>
        /// <remarks>
        /// The description will be presented to the user in the dialog when you'll try to access a related resource or sensor.
        /// </remarks>
        public string usageDescription
        {
            get => m_Description;
            set => m_Description = value;
        }

        [SerializeField] private bool m_Enabled;
        [SerializeField] private string m_Description;
    }
}

namespace UnityEngine.InputSystem
{
    public partial class InputSettings
    {
        /// <summary>
        /// Project-wide input settings for the iOS/tvOS platform.
        /// </summary>
        [Serializable]
        public class iOSSettings
        {
            /// <summary>
            /// Setting for access to the device's motion sensors (such as <see cref="StepCounter"/>).
            /// </summary>
            /// <remarks>
            /// Alternatively, you can manually add <c>Privacy - Motion Usage Description</c> to the Info.plist file.
            /// </remarks>
            /// <seealso cref="StepCounter"/>
            /// <seealso href="https://developer.apple.com/documentation/bundleresources/information_property_list/nsmotionusagedescription"/>
            public PrivacyDataUsage motionUsage
            {
                get => m_MotionUsage;
                set => m_MotionUsage = value;
            }

            [SerializeField] private PrivacyDataUsage m_MotionUsage = new PrivacyDataUsage();
        }

        /// <summary>
        /// iOS/tvOS-specific settings.
        /// </summary>
        /// <remarks>
        /// This is only accessible in the editor or in iOS/tvOS players.
        /// </remarks>
        public iOSSettings iOS => m_iOSSettings;

        [SerializeField]
        private iOSSettings m_iOSSettings = new iOSSettings();
    }
}

#endif
