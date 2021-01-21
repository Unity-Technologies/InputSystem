#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS || PACKAGE_DOCS_GENERATION
using System;
using UnityEngine.InputSystem.iOS;

namespace UnityEngine.InputSystem.iOS
{
    /// <summary>
    /// Describes privacy-sensitive data usage.
    /// </summary>
    [Serializable]
    public class PrivacyDataUsage
    {
        /// <summary>
        /// Toggle data usage.
        /// <remarks>
        /// Before accessing a resource or a sensor, you need to explicitly enable the usage for it, otherwise the access for the resource will be denied.
        /// </remarks>
        /// </summary>
        public bool Enabled
        {
            set => m_Enabled = value;
            get => m_Enabled;
        }

        /// <summary>
        /// Provide meaningful usage description.
        /// <remarks>
        /// The description will be present in the dialog when you'll try to access a related resource or sensor.
        /// </remarks>
        /// </summary>
        public string UsageDescription
        {
            set => m_Description = value;
            get => m_Description;
        }

        [SerializeField] private bool m_Enabled;
        [SerializeField] private string m_Description;
    }
}

namespace UnityEngine.InputSystem
{
    public partial class InputSettings : ScriptableObject
    {
        /// <summary>
        /// Project-wide input settings for iOS/tvOS platform.
        /// </summary>
        [Serializable]
        public class iOSSettings
        {
            /// <summary>
            /// Step Counter sensor usage description.
            /// </summary>
            public PrivacyDataUsage MotionUsage
            {
                set => m_MotionUsage = value;
                get => m_MotionUsage;
            }

            [SerializeField] private PrivacyDataUsage m_MotionUsage = new PrivacyDataUsage();
        }

        public iOSSettings iOS { get => m_iOSSettings; }

        [SerializeField]
        private iOSSettings m_iOSSettings = new iOSSettings();
    }
}

#endif
