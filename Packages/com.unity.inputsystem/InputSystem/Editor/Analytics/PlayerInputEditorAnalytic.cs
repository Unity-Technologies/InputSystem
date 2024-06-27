#if UNITY_EDITOR
using System;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Analytics for tracking Player Input component user engagement in the editor.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
        maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
    internal class PlayerInputEditorAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
    {
        public const string kEventName = "input_playerinput_editor";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        private readonly Data m_Data;

        public PlayerInputEditorAnalytic(ref Data data)
        {
            m_Data = data;
        }

        public InputAnalytics.InputAnalyticInfo info =>
            new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
        public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
        {
            data = m_Data;
            error = null;
            return true;
        }

        internal struct Data : IEquatable<Data>, UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            public InputEditorAnalytics.PlayerNotificationBehavior behavior;
            public bool has_actions;
            public bool has_default_map;
            public bool has_ui_input_module;
            public bool has_camera;

            public Data(PlayerInput playerInput)
            {
                behavior = InputEditorAnalytics.ToNotificationBehavior(playerInput.notificationBehavior);
                has_actions = playerInput.actions != null;
                has_default_map = playerInput.defaultActionMap != null;
                has_ui_input_module = playerInput.uiInputModule != null;
                has_camera = playerInput.camera != null;
            }

            public bool Equals(Data other)
            {
                return behavior == other.behavior &&
                    has_actions == other.has_actions &&
                    has_default_map == other.has_default_map &&
                    has_ui_input_module == other.has_ui_input_module &&
                    has_camera == other.has_camera;
            }

            public override bool Equals(object obj)
            {
                return obj is Data other && Equals(other);
            }

            public override int GetHashCode()
            {
                // Note: Not using HashCode.Combine since not available in older C# versions
                var hashCode = behavior.GetHashCode();
                hashCode = (hashCode * 397) ^ has_actions.GetHashCode();
                hashCode = (hashCode * 397) ^ has_default_map.GetHashCode();
                hashCode = (hashCode * 397) ^ has_ui_input_module.GetHashCode();
                hashCode = (hashCode * 397) ^ has_camera.GetHashCode();
                return hashCode;
            }
        }
    }
}
#endif
