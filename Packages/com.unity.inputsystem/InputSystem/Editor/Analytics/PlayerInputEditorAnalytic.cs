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
        public const string kEventName = "input_playerinput_editor_destroyed";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        private readonly UnityEditor.Editor m_Editor;

        public PlayerInputEditorAnalytic(UnityEditor.Editor editor)
        {
            m_Editor = editor;
        }

        public InputAnalytics.InputAnalyticInfo info =>
            new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
        public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
        {
            try
            {
                data = new Data(m_Editor.target as PlayerInput);
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
#if UNITY_INPUT_SYSTEM_ENABLE_UI
                has_ui_input_module = playerInput.uiInputModule != null;
#else
                has_ui_input_module = false;
#endif
                has_camera = playerInput.camera != null;
            }
        }
    }
}
#endif
