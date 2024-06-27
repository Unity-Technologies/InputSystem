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
        public const string kEventName = "inputPlayerInputEditor";
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
            public bool hasActions;
            public bool hasDefaultMap;
            public bool hasUIInputModule;
            public bool hasCamera;

            public Data(PlayerInput playerInput)
            {
                behavior = InputEditorAnalytics.ToNotificationBehavior(playerInput.notificationBehavior);
                hasActions = playerInput.actions != null;
                hasDefaultMap = playerInput.defaultActionMap != null;
                hasUIInputModule = playerInput.uiInputModule != null;
                hasCamera = playerInput.camera != null;
            }

            public bool Equals(Data other)
            {
                return behavior == other.behavior &&
                    hasActions == other.hasActions &&
                    hasDefaultMap == other.hasDefaultMap &&
                    hasUIInputModule == other.hasUIInputModule &&
                    hasCamera == other.hasCamera;
            }

            public override bool Equals(object obj)
            {
                return obj is Data other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine((int)behavior, hasActions, hasDefaultMap, hasUIInputModule, hasCamera);
            }
        }
    }
}
#endif
