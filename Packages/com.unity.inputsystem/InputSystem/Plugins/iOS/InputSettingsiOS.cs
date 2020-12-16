#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using System;

namespace UnityEngine.InputSystem
{
    public partial class InputSettings : ScriptableObject
    {
        [Serializable]
        public class iOSSettings
        {
            public bool MotionUsage
            {
                set => m_MotionUsage = value;
                get => m_MotionUsage;
            }

            public string MotionUsageDescription
            {
                set => m_MotionUsageDescription = value;
                get => m_MotionUsageDescription;
            }

            [SerializeField] private bool m_MotionUsage;
            [SerializeField] private string m_MotionUsageDescription;
        }

        public iOSSettings iOS { get => m_iOSSettings; }

        [SerializeField]
        private iOSSettings m_iOSSettings = new iOSSettings();
    }
}

#endif
