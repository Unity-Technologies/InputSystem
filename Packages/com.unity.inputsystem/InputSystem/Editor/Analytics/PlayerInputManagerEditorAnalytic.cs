#if UNITY_EDITOR
using System;

namespace UnityEngine.InputSystem.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
        maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
    internal class PlayerInputManagerEditorAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
    {
        public const string kEventName = "input_playerinputmanager_editor_destroyed";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        public InputAnalytics.InputAnalyticInfo info =>
            new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);

        private readonly UnityEditor.Editor m_Editor;

        public PlayerInputManagerEditorAnalytic(UnityEditor.Editor editor)
        {
            m_Editor = editor;
        }

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
        public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
        {
            try
            {
                data = new Data(m_Editor.target as PlayerInputManager);
                error = null;
            }
            catch (Exception e)
            {
                data = null;
                error = e;
            }
            return true;
        }

        internal struct Data : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            public enum PlayerJoinBehavior
            {
                JoinPlayersWhenButtonIsPressed = 0, // default
                JoinPlayersWhenJoinActionIsTriggered = 1,
                JoinPlayersManually = 2
            }

            public InputEditorAnalytics.PlayerNotificationBehavior behavior;
            public PlayerJoinBehavior join_behavior;
            public bool joining_enabled_by_default;
            public int max_player_count;

            public Data(PlayerInputManager value)
            {
                behavior = InputEditorAnalytics.ToNotificationBehavior(value.notificationBehavior);
                join_behavior = ToPlayerJoinBehavior(value.joinBehavior);
                joining_enabled_by_default = value.joiningEnabled;
                max_player_count = value.maxPlayerCount;
            }

            private static PlayerJoinBehavior ToPlayerJoinBehavior(UnityEngine.InputSystem.PlayerJoinBehavior value)
            {
                switch (value)
                {
                    case UnityEngine.InputSystem.PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed:
                        return PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
                    case UnityEngine.InputSystem.PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered:
                        return PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;
                    case UnityEngine.InputSystem.PlayerJoinBehavior.JoinPlayersManually:
                        return PlayerJoinBehavior.JoinPlayersManually;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }
            }
        }
    }
}
#endif
