#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ISX
{
    // Allows looking at input activity in the editor.
    // Can display either local input in editor or input activity in connected player or both.
    internal class InputDebuggerWindow : EditorWindow, ISerializationCallbackReceiver
    {
        private static InputDebuggerWindow s_Instance;

        [MenuItem("Window/Input Debugger", false, 2100)]
        public static void Init()
        {
            if (s_Instance != null)
            {
                s_Instance = GetWindow<InputDebuggerWindow>();
                s_Instance.Show();
                s_Instance.titleContent = new GUIContent("Input Debugger");
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            Repaint();
        }

        public void Awake()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        public void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
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
            GUILayout.FlexibleSpace();
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

            for (var i = 0; i < deviceCount; i++)
            {
                var device = devices[i];
                var deviceIndex = device.m_DeviceIndex;

                // Skip devices that aren't in the connection state we're looking for.
                if (device.connected != connected)
                    continue;
                ++displayedDeviceCount;

                // Draw it.
                var rect = GUILayoutUtility.GetRect(new GUIContent(device.name), Styles.deviceStyle, GUILayout.Width(kDeviceElementWidth));
                DrawDevice(device, rect);
            }

            if (displayedDeviceCount == 0)
                EditorGUILayout.LabelField(Contents.noneContent);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDevice(InputDevice device, Rect position)
        {
            var deviceContent = new GUIContent(device.name);
            var width = Styles.deviceStyle.CalcSize(deviceContent).x;
            var textIsClipped = width > position.width;
            if (GUI.Button(position, deviceContent, textIsClipped ? Styles.deviceStyleClipped : Styles.deviceStyle))
                InputDeviceDebuggerWindow.CreateOrShowExisting(device);
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

        [NonSerialized] private List<InputDeviceDescription> m_UnrecognizedDevices;

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

            // Revive individual device windows.
            InputDeviceDebuggerWindow.ReviveAfterDomainReload();
        }

        private static class Styles
        {
            public static GUIStyle deviceStyle = new GUIStyle("button");
            public static GUIStyle deviceStyleClipped = new GUIStyle(deviceStyle) {alignment = TextAnchor.MiddleLeft};
        }

        private static class Contents
        {
            public static GUIContent noneContent = new GUIContent("None");
            public static GUIContent connectedDevicesContent = new GUIContent("Connected Devices");
            public static GUIContent disconnectedDevicesContent = new GUIContent("Disconnected Devices");
            public static GUIContent unrecognizedDevicesContent = new GUIContent("Unrecognized Devices");
            public static GUIContent showUnrecognizedDevicesContent = new GUIContent("Show Unrecognized Devices");
            public static GUIContent showDisconnectedDevicesContent = new GUIContent("Show Disconnected Devices");
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
