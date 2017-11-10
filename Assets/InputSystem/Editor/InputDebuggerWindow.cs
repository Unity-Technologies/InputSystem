#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

////TODO: Ideally, I'd like all separate EditorWindows opened by the InputDebugger to automatically
////      be docked into the container window of InputDebuggerWindow

////REVIVEW: make device and/or action list a treeview?

namespace ISX.Editor
{
    // Allows looking at input activity in the editor.
    // Can display either local input in editor or input activity in connected player or both.
    internal class InputDebuggerWindow : EditorWindow, ISerializationCallbackReceiver, IHasCustomMenu
    {
        private static InputDebuggerWindow s_Instance;

        [MenuItem("Window/Input Debugger", false, 2100)]
        public static void Init()
        {
            if (s_Instance == null)
            {
                s_Instance = GetWindow<InputDebuggerWindow>();
                s_Instance.Show();
                s_Instance.titleContent = new GUIContent("Input Debugger");
            }
            else
            {
                s_Instance.Show();
                s_Instance.Focus();
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            Repaint();
        }

        public void Awake()
        {
            InputSystem.onDeviceChange += OnDeviceChange;

            if (InputActionSet.s_OnEnabledActionsChanged == null)
                InputActionSet.s_OnEnabledActionsChanged = new List<Action>();
            InputActionSet.s_OnEnabledActionsChanged.Add(Repaint);
        }

        public void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            if (InputActionSet.s_OnEnabledActionsChanged != null)
                InputActionSet.s_OnEnabledActionsChanged.Remove(Repaint);
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Contents.lockInputToGameContent, InputConfiguration.LockInputToGame, ToggleLockInputToGame);
        }

        private void ToggleLockInputToGame()
        {
            InputConfiguration.LockInputToGame = !InputConfiguration.LockInputToGame;
        }

        public void OnGUI()
        {
            DrawToolbarGUI();

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            DrawDevicesGUI(Contents.connectedDevicesContent, true);
            if (m_ShowDisconnectedDevices)
                DrawDevicesGUI(Contents.disconnectedDevicesContent, false);

            if (m_ShowUnrecognizedDevices)
                DrawUnrecognizedDevicesGUI();

            DrawActionsGUI();

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_ShowUnrecognizedDevices = GUILayout.Toggle(m_ShowUnrecognizedDevices,
                    Contents.showUnrecognizedDevicesContent, EditorStyles.toolbarButton);
            m_ShowDisconnectedDevices = GUILayout.Toggle(m_ShowDisconnectedDevices,
                    Contents.showDisconnectedDevicesContent, EditorStyles.toolbarButton);
            m_ShowDisabledActions = GUILayout.Toggle(m_ShowDisabledActions,
                    Contents.showDisabledActionsContent, EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Contents.browseTemplatesContent, EditorStyles.toolbarButton))
                InputTemplateBrowserWindow.CreateOrShowExisting();
            EditorGUILayout.EndHorizontal();
        }

        // Draw a button for each device. Clicking the button pops up an InputDeviceDebuggerWindow
        // on the device.
        private void DrawDevicesGUI(GUIContent label, bool connected)
        {
            var devices = InputSystem.devices;
            var deviceCount = devices.Count;

            GUILayout.Label(label, EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));

            var displayedDeviceCount = 0;

            ////TODO: make buttons overflow into subsequent lines if they don't fit across width

            for (var i = 0; i < deviceCount; i++)
            {
                var device = devices[i];

                // Skip devices that aren't in the connection state we're looking for.
                if (device.connected != connected)
                    continue;
                ++displayedDeviceCount;

                // Decide whether to show product name or device name.
                var text = device.description.product;
                if (string.IsNullOrEmpty(text))
                    text = device.name;

                // Draw it.
                var deviceLabel = new GUIContent(text);
                var rect = GUILayoutUtility.GetRect(deviceLabel, Styles.deviceStyle, GUILayout.Width(kDeviceElementWidth));
                var width = Styles.deviceStyle.CalcSize(deviceLabel).x;
                var textOverflowingButton = width > rect.width;

                if (rect.x + rect.width >= position.width)
                {
                    ////FIXME: this does not work; Unity just throws a bunch of exceptions when trying to break horizontal groups like this
                    //EditorGUILayout.EndHorizontal();
                    //EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                }

                if (GUI.Button(rect, deviceLabel, textOverflowingButton ? Styles.deviceStyleLeftAligned : Styles.deviceStyle))
                    InputDeviceDebuggerWindow.CreateOrShowExisting(device);
            }

            if (displayedDeviceCount == 0)
                EditorGUILayout.LabelField(Contents.noneContent);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawUnrecognizedDevicesGUI()
        {
            GUILayout.Label(Contents.unrecognizedDevicesContent, EditorStyles.boldLabel);

            // Fetch list of unrecognized devices.
            if (m_UnrecognizedDevices == null)
                m_UnrecognizedDevices = new List<InputDeviceDescription>();
            m_UnrecognizedDevices.Clear();
            InputSystem.GetUnrecognizedDevices(m_UnrecognizedDevices);

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));

            var numUnrecognized = m_UnrecognizedDevices.Count;
            if (numUnrecognized == 0)
            {
                GUILayout.Label(Contents.noneContent);
            }
            else
            {
                for (var i = 0; i < numUnrecognized; ++i)
                    GUILayout.Label(m_UnrecognizedDevices[i].ToString());
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionsGUI()
        {
            GUILayout.Label(Contents.enabledActionsContent, EditorStyles.boldLabel);

            if (m_EnabledActions == null)
                m_EnabledActions = new List<InputAction>();
            else
                m_EnabledActions.Clear();

            InputSystem.FindAllEnabledActions(m_EnabledActions);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));

            var numEnabledActions = m_EnabledActions.Count;
            if (numEnabledActions == 0)
            {
                GUILayout.Label(Contents.noneContent);
            }
            else
            {
                for (var i = 0; i < m_EnabledActions.Count; ++i)
                {
                    var action = m_EnabledActions[i];
                    if (GUILayout.Button(action.name))
                    {
                        InputActionDebuggerWindow.CreateOrShowExisting(m_EnabledActions[i]);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // Whether to display devices from a player connection or not.
        private enum Mode
        {
            Both,
            LocalOnly,
            RemoteOnly
        }

        private const int kDeviceElementWidth = 150;

        [SerializeField] private Mode m_Mode;
        [SerializeField] private Vector2 m_ScrollPosition;
        [SerializeField] private bool m_ShowUnrecognizedDevices;
        [SerializeField] private bool m_ShowDisconnectedDevices;
        [SerializeField] private bool m_ShowDisabledActions;

        [NonSerialized] private List<InputDeviceDescription> m_UnrecognizedDevices;
        [NonSerialized] private List<InputAction> m_EnabledActions;

        internal static void ReviveAfterDomainReload()
        {
            // ATM the onDeviceChange UnityEvent does not seem to properly survive reloads
            // so we hook back in here.
            if (s_Instance != null)
            {
                InputSystem.onDeviceChange += s_Instance.OnDeviceChange;

                // Trigger an initial repaint now that we know the input system has come
                // back to life.
                s_Instance.Repaint();
            }
        }

        private static class Styles
        {
            public static GUIStyle deviceStyle = new GUIStyle("button");
            public static GUIStyle deviceStyleLeftAligned = new GUIStyle(deviceStyle) {alignment = TextAnchor.MiddleLeft};
        }

        private static class Contents
        {
            public static GUIContent noneContent = new GUIContent("None");
            public static GUIContent connectedDevicesContent = new GUIContent("Connected Devices");
            public static GUIContent disconnectedDevicesContent = new GUIContent("Disconnected Devices");
            public static GUIContent unrecognizedDevicesContent = new GUIContent("Unrecognized Devices");
            public static GUIContent showUnrecognizedDevicesContent = new GUIContent("Show Unrecognized Devices");
            public static GUIContent showDisconnectedDevicesContent = new GUIContent("Show Disconnected Devices");
            public static GUIContent showDisabledActionsContent = new GUIContent("Show Disabled Actions");
            public static GUIContent lockInputToGameContent = new GUIContent("Lock Input to Game");
            public static GUIContent browseTemplatesContent = new GUIContent("Browse Templates");
            public static GUIContent enabledActionsContent = new GUIContent("Enabled Actions");
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            s_Instance = this;
        }
    }
}
#endif // UNITY_EDITOR
