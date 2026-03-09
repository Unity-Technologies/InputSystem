#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Users;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="PlayerInputManager"/>.
    /// </summary>
    [CustomEditor(typeof(PlayerInputManager))]
    internal class PlayerInputManagerEditor : UnityEditor.Editor
    {
        public void OnEnable()
        {
            InputUser.onChange += OnUserChange;
            CacheProperties();
        }

        private void CacheProperties()
        {
            m_NotificationBehaviorProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_NotificationBehavior));
            m_PlayerJoinedEventProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_PlayerJoinedEvent));
            m_PlayerLeftEventProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_PlayerLeftEvent));
            m_JoinBehaviorProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_JoinBehavior));
            m_JoinActionProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_JoinAction));
            m_PlayerPrefabProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_PlayerPrefab));
            m_AllowJoiningProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_AllowJoining));
            m_MaxPlayerCountProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_MaxPlayerCount));
            m_SplitScreenProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_SplitScreen));
            m_MaintainAspectRatioProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_MaintainAspectRatioInSplitScreen));
            m_FixedNumberOfSplitScreensProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_FixedNumberOfSplitScreens));
            m_SplitScreenRectProperty = serializedObject.FindProperty(nameof(PlayerInputManager.m_SplitScreenRect));
        }

        public void OnDisable()
        {
            new InputComponentEditorAnalytic(InputSystemComponent.PlayerInputManager).Send();
            new PlayerInputManagerEditorAnalytic(this).Send();
        }

        public void OnDestroy()
        {
            InputUser.onChange -= OnUserChange;
        }

        private void OnUserChange(InputUser user, InputUserChange change, InputDevice device)
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            DoNotificationSectionUI();
            EditorGUILayout.Space();
            DoJoinSectionUI();
            EditorGUILayout.Space();
            DoSplitScreenSectionUI();

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (EditorApplication.isPlaying)
                DoDebugUI();
        }

        private void DoNotificationSectionUI()
        {
            EditorGUILayout.PropertyField(m_NotificationBehaviorProperty);
            switch ((PlayerNotifications)m_NotificationBehaviorProperty.intValue)
            {
                case PlayerNotifications.SendMessages:
                    if (m_SendMessagesHelpText == null)
                        m_SendMessagesHelpText = EditorGUIUtility.TrTextContent(
                            $"Will SendMessage() to GameObject: " + string.Join(",", PlayerInputManager.messages));
                    EditorGUILayout.HelpBox(m_SendMessagesHelpText);
                    break;

                case PlayerNotifications.BroadcastMessages:
                    if (m_BroadcastMessagesHelpText == null)
                        m_BroadcastMessagesHelpText = EditorGUIUtility.TrTextContent(
                            $"Will BroadcastMessage() to GameObject: " + string.Join(",", PlayerInputManager.messages));
                    EditorGUILayout.HelpBox(m_BroadcastMessagesHelpText);
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                    m_EventsExpanded = EditorGUILayout.Foldout(m_EventsExpanded, m_EventsLabel, toggleOnLabelClick: true);
                    if (m_EventsExpanded)
                    {
                        EditorGUILayout.PropertyField(m_PlayerJoinedEventProperty);
                        EditorGUILayout.PropertyField(m_PlayerLeftEventProperty);
                    }
                    break;
            }
        }

        private void DoJoinSectionUI()
        {
            EditorGUILayout.LabelField(m_JoiningGroupLabel, EditorStyles.boldLabel);

            // Join behavior
            EditorGUILayout.PropertyField(m_JoinBehaviorProperty);
            if ((PlayerJoinBehavior)m_JoinBehaviorProperty.intValue != PlayerJoinBehavior.JoinPlayersManually)
            {
                ++EditorGUI.indentLevel;

                // Join action.
                if ((PlayerJoinBehavior)m_JoinBehaviorProperty.intValue ==
                    PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered)
                {
                    EditorGUILayout.PropertyField(m_JoinActionProperty);
                }

                // Player prefab.
                EditorGUILayout.PropertyField(m_PlayerPrefabProperty);

                ValidatePlayerPrefab(m_JoinBehaviorProperty, m_PlayerPrefabProperty);

                --EditorGUI.indentLevel;
            }

            // Enabled-by-default.
            if (m_AllowingJoiningLabel == null)
                m_AllowingJoiningLabel = new GUIContent("Joining Enabled By Default", m_AllowJoiningProperty.GetTooltip());
            EditorGUILayout.PropertyField(m_AllowJoiningProperty, m_AllowingJoiningLabel);

            // Max player count.
            if (m_EnableMaxPlayerCountLabel == null)
                m_EnableMaxPlayerCountLabel = EditorGUIUtility.TrTextContent("Limit Number of Players", m_MaxPlayerCountProperty.GetTooltip());
            if (m_MaxPlayerCountProperty.intValue > 0)
                m_MaxPlayerCountEnabled = true;
            m_MaxPlayerCountEnabled = EditorGUILayout.Toggle(m_EnableMaxPlayerCountLabel, m_MaxPlayerCountEnabled);
            if (m_MaxPlayerCountEnabled)
            {
                ++EditorGUI.indentLevel;
                if (m_MaxPlayerCountProperty.intValue < 0)
                    m_MaxPlayerCountProperty.intValue = 1;
                EditorGUILayout.PropertyField(m_MaxPlayerCountProperty);
                --EditorGUI.indentLevel;
            }
            else
                m_MaxPlayerCountProperty.intValue = -1;
        }

        private static void ValidatePlayerPrefab(SerializedProperty joinBehaviorProperty,
            SerializedProperty playerPrefabProperty)
        {
            if ((PlayerJoinBehavior)joinBehaviorProperty.intValue != PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed)
                return;

            if (playerPrefabProperty.objectReferenceValue == null)
                return;

            var playerInput = ((GameObject)playerPrefabProperty.objectReferenceValue)
                .GetComponentInChildren<PlayerInput>();

            if (playerInput == null)
            {
                EditorGUILayout.HelpBox("No PlayerInput component found in player prefab.", MessageType.Info);
                return;
            }

            if (playerInput.actions == null)
            {
                EditorGUILayout.HelpBox("PlayerInput component has no input action asset assigned.", MessageType.Info);
                return;
            }

            if (playerInput.actions.controlSchemes.Any(c => c.deviceRequirements.Count > 0) == false)
                EditorGUILayout.HelpBox("Join Players When Button Is Pressed behavior will not work when the Input Action Asset " +
                    "assigned to the PlayerInput component has no required devices in any control scheme.",
                    MessageType.Info);
        }

        private void DoSplitScreenSectionUI()
        {
            EditorGUILayout.LabelField(m_SplitScreenGroupLabel, EditorStyles.boldLabel);

            // Split-screen toggle.
            if (m_SplitScreenLabel == null)
                m_SplitScreenLabel = new GUIContent("Enable Split-Screen", m_SplitScreenProperty.GetTooltip());
            EditorGUILayout.PropertyField(m_SplitScreenProperty, m_SplitScreenLabel);
            if (!m_SplitScreenProperty.boolValue)
                return;

            ++EditorGUI.indentLevel;

            // Maintain-aspect-ratio toggle.
            if (m_MaintainAspectRatioLabel == null)
                m_MaintainAspectRatioLabel =
                    new GUIContent("Maintain Aspect Ratio", m_MaintainAspectRatioProperty.GetTooltip());
            EditorGUILayout.PropertyField(m_MaintainAspectRatioProperty, m_MaintainAspectRatioLabel);

            // Fixed-number toggle.
            if (m_EnableFixedNumberOfSplitScreensLabel == null)
                m_EnableFixedNumberOfSplitScreensLabel = EditorGUIUtility.TrTextContent("Set Fixed Number", m_FixedNumberOfSplitScreensProperty.GetTooltip());
            if (m_FixedNumberOfSplitScreensProperty.intValue > 0)
                m_FixedNumberOfSplitScreensEnabled = true;
            m_FixedNumberOfSplitScreensEnabled = EditorGUILayout.Toggle(m_EnableFixedNumberOfSplitScreensLabel,
                m_FixedNumberOfSplitScreensEnabled);
            if (m_FixedNumberOfSplitScreensEnabled)
            {
                ++EditorGUI.indentLevel;
                if (m_FixedNumberOfSplitScreensProperty.intValue < 0)
                    m_FixedNumberOfSplitScreensProperty.intValue = 4;
                if (m_FixedNumberOfSplitScreensLabel == null)
                    m_FixedNumberOfSplitScreensLabel = EditorGUIUtility.TrTextContent("Number of Screens",
                        m_FixedNumberOfSplitScreensProperty.tooltip);
                EditorGUILayout.PropertyField(m_FixedNumberOfSplitScreensProperty, m_FixedNumberOfSplitScreensLabel);
                --EditorGUI.indentLevel;
            }
            else
            {
                m_FixedNumberOfSplitScreensProperty.intValue = -1;
            }

            // Split-screen area.
            if (m_SplitScreenAreaLabel == null)
                m_SplitScreenAreaLabel = new GUIContent("Screen Rectangle", m_SplitScreenRectProperty.GetTooltip());
            EditorGUILayout.PropertyField(m_SplitScreenRectProperty, m_SplitScreenAreaLabel);

            --EditorGUI.indentLevel;
        }

        private void DoDebugUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_DebugLabel, EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);

            var players = PlayerInput.all;
            if (players.Count == 0)
            {
                EditorGUILayout.LabelField("No Players");
            }
            else
            {
                foreach (var player in players)
                {
                    var str = player.gameObject.name;
                    if (player.splitScreenIndex != -1)
                        str += $" (Screen #{player.splitScreenIndex})";
                    EditorGUILayout.LabelField("Player #" + player.playerIndex, str);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        [SerializeField] private bool m_EventsExpanded;
        [SerializeField] private bool m_MaxPlayerCountEnabled;
        [SerializeField] private bool m_FixedNumberOfSplitScreensEnabled;

        [NonSerialized] private SerializedProperty m_NotificationBehaviorProperty;
        [NonSerialized] private SerializedProperty m_PlayerJoinedEventProperty;
        [NonSerialized] private SerializedProperty m_PlayerLeftEventProperty;
        [NonSerialized] private SerializedProperty m_JoinBehaviorProperty;
        [NonSerialized] private SerializedProperty m_JoinActionProperty;
        [NonSerialized] private SerializedProperty m_PlayerPrefabProperty;
        [NonSerialized] private SerializedProperty m_AllowJoiningProperty;
        [NonSerialized] private SerializedProperty m_MaxPlayerCountProperty;
        [NonSerialized] private SerializedProperty m_SplitScreenProperty;
        [NonSerialized] private SerializedProperty m_MaintainAspectRatioProperty;
        [NonSerialized] private SerializedProperty m_FixedNumberOfSplitScreensProperty;
        [NonSerialized] private SerializedProperty m_SplitScreenRectProperty;

        [NonSerialized] private readonly GUIContent m_JoiningGroupLabel = EditorGUIUtility.TrTextContent("Joining");
        [NonSerialized] private readonly GUIContent m_SplitScreenGroupLabel = EditorGUIUtility.TrTextContent("Split-Screen");
        [NonSerialized] private readonly GUIContent m_EventsLabel = EditorGUIUtility.TrTextContent("Events");
        [NonSerialized] private readonly GUIContent m_DebugLabel = EditorGUIUtility.TrTextContent("Debug");
        [NonSerialized] private GUIContent m_SendMessagesHelpText;
        [NonSerialized] private GUIContent m_BroadcastMessagesHelpText;
        [NonSerialized] private GUIContent m_AllowingJoiningLabel;
        [NonSerialized] private GUIContent m_SplitScreenLabel;
        [NonSerialized] private GUIContent m_MaintainAspectRatioLabel;
        [NonSerialized] private GUIContent m_SplitScreenAreaLabel;
        [NonSerialized] private GUIContent m_FixedNumberOfSplitScreensLabel;
        [NonSerialized] private GUIContent m_EnableMaxPlayerCountLabel;
        [NonSerialized] private GUIContent m_EnableFixedNumberOfSplitScreensLabel;
    }
}
#endif // UNITY_EDITOR
