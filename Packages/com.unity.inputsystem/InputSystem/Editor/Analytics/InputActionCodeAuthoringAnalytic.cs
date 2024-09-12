#if UNITY_EDITOR

using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
        maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
    internal class InputActionCodeAuthoringAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
    {
        public const string kEventName = "input_action_code_authoring";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        /// <summary>
        /// Enumeration type for code authoring APIs mapping to <see cref="InputActionSetupExtensions"/>.
        /// </summary>
        /// <remarks>
        /// This enumeration type may be added to, but NEVER changed, since it would break older data.
        /// </remarks>
        public enum Api
        {
            AddBinding = 0,
            AddCompositeBinding = 1,
            ChangeBinding = 2,
            ChangeCompositeBinding = 3,
            Rename = 4,
            AddControlScheme = 5,
            RemoveControlScheme = 6,
            ControlSchemeWithBindingGroup = 7,
            ControlSchemeWithDevice = 8,
            ControlSchemeWithRequiredDevice = 9,
            ControlSchemeWithOptionalDevice = 10,
            ControlSchemeOrWithRequiredDevice = 11,
            ControlSchemeOrWithOptionalDevice = 12
        }

        private static readonly int[] m_Counters = new int[Enum.GetNames(typeof(Api)).Length];

        public static void Register(Api api)
        {
            // Note: Currently discards detailed information and only sets a boolean (aggregated) value.
            ++m_Counters[(int)api];
        }

        // Cache delegate
        private static readonly Action<PlayModeStateChange> PlayModeChanged = OnPlayModeStateChange;

        private static void OnPlayModeStateChange(PlayModeStateChange change)
        {
            Debug.Log("Play mode state change: " + change);
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                // Reset all counters when exiting edit mode
                for (var i = 0; i < m_Counters.Length; ++i)
                    m_Counters[i] = 0;
            }
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                // Send analytics and unhook delegate when exiting play-mode
                EditorApplication.playModeStateChanged -= PlayModeChanged;
                new InputActionCodeAuthoringAnalytic().Send();
            }
        }

        [InitializeOnEnterPlayMode]
        private static void Hook()
        {
            // Make sure only a single play-mode change delegate is registered
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        private InputActionCodeAuthoringAnalytic()
        {
            info = new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);
        }

        /// <summary>
        /// Represents InputAction code authoring editor data.
        /// </summary>
        /// <remarks>
        /// Ideally this struct should be readonly but then Unity cannot serialize/deserialize it.
        /// </remarks>
        [Serializable]
        public struct Data : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            /// <summary>
            /// Creates a new <c>Data</c> instance.
            /// </summary>
            /// <param name="usesCodeAuthoring">Specifies whether code authoring has been used.</param>
            public Data(bool usesCodeAuthoring)
            {
                this.usesCodeAuthoring = usesCodeAuthoring;
            }

            /// <summary>
            /// Specifies whether code-authoring (Input Action setup via extensions) was used at least once
            /// during play-mode.
            /// </summary>
            public bool usesCodeAuthoring;
        }

#if UNITY_2023_2_OR_NEWER
        public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
        public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
        {
            try
            {
                // Determine aggregated perspective, i.e. was "any" code-authoring API used
                var usedCodeAuthoringDuringPlayMode = false;
                for (var i = 0; i < m_Counters.Length; ++i)
                {
                    if (m_Counters[i] > 0)
                    {
                        usedCodeAuthoringDuringPlayMode = true;
                        break;
                    }
                }

                data = new Data(usedCodeAuthoringDuringPlayMode);
                error = null;
                return true;
            }
            catch (Exception e)
            {
                data = null;
                error = e;
                return false;
            }
        }

        public InputAnalytics.InputAnalyticInfo info { get; }
    }
}

#endif // UNITY_EDITOR
