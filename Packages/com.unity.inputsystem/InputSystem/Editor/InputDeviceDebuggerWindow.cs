#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////FIXME: doesn't survive domain reload correctly

////FIXME: the repaint triggered from IInputStateCallbackReceiver somehow comes with a significant delay

////TODO: Add "Remote:" field in list that also has a button for local devices that allows to mirror them and their input
////      into connected players

////TODO: this window should help diagnose problems in the event stream (e.g. ignored state events and why they were ignored)

////TODO: add toggle to that switches to displaying raw control values

////TODO: allow adding visualizers (or automatically add them in cases) to control that show value over time (using InputStateHistory)

////TODO: show default states of controls

namespace UnityEngine.Experimental.Input.Editor
{
    // Shows status and activity of a single input device in a separate window.
    // Can also be used to alter the state of a device by making up state events.
    public class InputDeviceDebuggerWindow : EditorWindow, ISerializationCallbackReceiver
    {
        internal const int kMaxNumEventsInTrace = 64;

        internal static InlinedArray<Action<InputDevice>> s_OnToolbarGUIActions;

        public static event Action<InputDevice> onToolbarGUI
        {
            add { s_OnToolbarGUIActions.Append(value); }
            remove { s_OnToolbarGUIActions.Remove(value); }
        }

        public static void CreateOrShowExisting(InputDevice device)
        {
            // See if we have an existing window for the device and if so pop it
            // in front.
            if (s_OpenDebuggerWindows != null)
            {
                for (var i = 0; i < s_OpenDebuggerWindows.Count; ++i)
                {
                    var existingWindow = s_OpenDebuggerWindows[i];
                    if (existingWindow.m_DeviceId == device.id)
                    {
                        existingWindow.Show();
                        existingWindow.Focus();
                        return;
                    }
                }
            }

            // No, so create a new one.
            var window = CreateInstance<InputDeviceDebuggerWindow>();
            window.InitializeWith(device);
            window.minSize = new Vector2(270, 300);
            window.Show();
            window.titleContent = new GUIContent(device.name);
        }

        internal void OnDestroy()
        {
            if (m_Device != null)
            {
                RemoveFromList();

                if (m_EventTrace != null)
                    m_EventTrace.Dispose();

                InputSystem.onDeviceChange -= OnDeviceChange;
            }
        }

        internal void OnGUI()
        {
            // Find device again if we've gone through a domain reload.
            if (m_Device == null)
            {
                m_Device = InputSystem.GetDeviceById(m_DeviceId);

                if (m_Device == null)
                {
                    EditorGUILayout.HelpBox(Styles.notFoundHelpText, MessageType.Warning);
                    return;
                }

                InitializeWith(m_Device);
            }

            ////FIXME: with ExpandHeight(false), editor still expands height for some reason....
            EditorGUILayout.BeginVertical("OL Box", GUILayout.Height(170));// GUILayout.ExpandHeight(false));
            EditorGUILayout.LabelField("Name", m_Device.name);
            EditorGUILayout.LabelField("Layout", m_Device.layout);
            EditorGUILayout.LabelField("Type", m_Device.GetType().Name);
            EditorGUILayout.LabelField("Interface", m_Device.description.interfaceName);
            EditorGUILayout.LabelField("Product", m_Device.description.product);
            EditorGUILayout.LabelField("Manufacturer", m_Device.description.manufacturer);
            EditorGUILayout.LabelField("Serial Number", m_Device.description.serial);
            EditorGUILayout.LabelField("Device ID", m_DeviceIdString);
            EditorGUILayout.LabelField("Usages", m_DeviceUsagesString);
            EditorGUILayout.LabelField("Flags", m_DeviceFlagsString);
            EditorGUILayout.EndVertical();

            DrawControlTree();
            DrawEventList();
        }

        private void DrawControlTree()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Controls", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            // Allow plugins to add toolbar buttons.
            for (var i = 0; i < s_OnToolbarGUIActions.length; ++i)
                s_OnToolbarGUIActions[i](m_Device);

            if (GUILayout.Button(Contents.stateContent, EditorStyles.toolbarButton))
            {
                var window = CreateInstance<InputStateWindow>();
                window.InitializeWithControl(m_Device);
                window.Show();
            }

            GUILayout.EndHorizontal();

            ////TODO: detect if dynamic is disabled and fall back to fixed
            var updateTypeToShow = EditorApplication.isPlaying ? InputUpdateType.Dynamic : InputUpdateType.Editor;

            try
            {
                // Switch to buffers that we want to display in the control tree.
                InputStateBuffers.SwitchTo(InputSystem.s_Manager.m_StateBuffers, updateTypeToShow);

                ////REVIEW: I'm not sure tree view needs a scroll view or whether it does that automatically
                m_ControlTreeScrollPosition = EditorGUILayout.BeginScrollView(m_ControlTreeScrollPosition);
                var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_ControlTree.OnGUI(rect);
                EditorGUILayout.EndScrollView();
            }
            finally
            {
                // Switch back to editor buffers.
                InputStateBuffers.SwitchTo(InputSystem.s_Manager.m_StateBuffers, InputUpdateType.Editor);
            }
        }

        private void DrawEventList()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Events", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Contents.clearContent, EditorStyles.toolbarButton))
            {
                m_EventTrace.Clear();
                m_EventTree.Reload();
            }

            var eventTraceDisabledNow = GUILayout.Toggle(!m_EventTraceDisabled, Contents.pauseContent, EditorStyles.toolbarButton);
            if (eventTraceDisabledNow != m_EventTraceDisabled)
            {
                m_EventTraceDisabled = eventTraceDisabledNow;
                if (eventTraceDisabledNow)
                    m_EventTrace.Disable();
                else
                    m_EventTrace.Enable();
            }

            GUILayout.EndHorizontal();

            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_EventTree.OnGUI(rect);
        }

        ////FIXME: some of the state in here doesn't get refreshed when it's changed on the device
        private void InitializeWith(InputDevice device)
        {
            m_Device = device;
            m_DeviceId = device.id;
            m_DeviceIdString = device.id.ToString();
            m_DeviceUsagesString = string.Join(", ", device.usages.Select(x => x.ToString()).ToArray());

            var flags = new List<string>();
            if ((m_Device.m_DeviceFlags & InputDevice.DeviceFlags.Native) == InputDevice.DeviceFlags.Native)
                flags.Add("Native");
            if ((m_Device.m_DeviceFlags & InputDevice.DeviceFlags.Remote) == InputDevice.DeviceFlags.Remote)
                flags.Add("Remote");
            if ((m_Device.m_DeviceFlags & InputDevice.DeviceFlags.UpdateBeforeRender) == InputDevice.DeviceFlags.UpdateBeforeRender)
                flags.Add("UpdateBeforeRender");
            if ((m_Device.m_DeviceFlags & InputDevice.DeviceFlags.HasStateCallbacks) == InputDevice.DeviceFlags.HasStateCallbacks)
                flags.Add("HasStateCallbacks");
            if ((m_Device.m_DeviceFlags & InputDevice.DeviceFlags.Disabled) == InputDevice.DeviceFlags.Disabled)
                flags.Add("Disabled");
            m_DeviceFlagsString = string.Join(", ", flags.ToArray());

            // Set up event trace. The default trace size of 1mb fits a ton of events and will
            // likely bog down the UI if we try to display that many events. Instead, come up
            // with a more reasonable sized based on the state size of the device.
            if (m_EventTrace == null)
                m_EventTrace = new InputEventTrace((int)device.stateBlock.alignedSizeInBytes * kMaxNumEventsInTrace) {deviceId = device.id};
            m_EventTrace.onEvent += _ =>
            {
                ////FIXME: this is very inefficient
                m_EventTree.Reload();
            };
            if (!m_EventTraceDisabled)
                m_EventTrace.Enable();

            // Set up event tree.
            m_EventTree = InputEventTreeView.Create(m_Device, m_EventTrace, ref m_EventTreeState, ref m_EventTreeHeaderState);

            // Set up control tree.
            m_ControlTree = InputControlTreeView.Create(m_Device, 1, ref m_ControlTreeState, ref m_ControlTreeHeaderState);
            m_ControlTree.Reload();
            m_ControlTree.ExpandAll();

            AddToList();
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        // We will lose our device on domain reload and then look it back up the first
        // time we hit a repaint after a reload. By that time, the input system should have
        // fully come back to life as well.
        [NonSerialized] private InputDevice m_Device;
        [NonSerialized] private string m_DeviceIdString;
        [NonSerialized] private string m_DeviceUsagesString;
        [NonSerialized] private string m_DeviceFlagsString;
        [NonSerialized] private InputControlTreeView m_ControlTree;
        [NonSerialized] private InputEventTreeView m_EventTree;

        [SerializeField] private int m_DeviceId = InputDevice.kInvalidDeviceId;
        [SerializeField] private TreeViewState m_ControlTreeState;
        [SerializeField] private TreeViewState m_EventTreeState;
        [SerializeField] private MultiColumnHeaderState m_ControlTreeHeaderState;
        [SerializeField] private MultiColumnHeaderState m_EventTreeHeaderState;
        [SerializeField] private Vector2 m_ControlTreeScrollPosition;
        [SerializeField] private InputEventTrace m_EventTrace;
        [SerializeField] private bool m_EventTraceDisabled;

        private static List<InputDeviceDebuggerWindow> s_OpenDebuggerWindows;

        private void AddToList()
        {
            if (s_OpenDebuggerWindows == null)
                s_OpenDebuggerWindows = new List<InputDeviceDebuggerWindow>();
            if (!s_OpenDebuggerWindows.Contains(this))
                s_OpenDebuggerWindows.Add(this);
        }

        private void RemoveFromList()
        {
            if (s_OpenDebuggerWindows != null)
                s_OpenDebuggerWindows.Remove(this);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device.id != m_DeviceId)
                return;

            if (change == InputDeviceChange.Removed)
            {
                Close();
            }
            else
            {
                Repaint();

                // If the state of the device changed, refresh control values in the control tree.
                if (change == InputDeviceChange.StateChanged && m_ControlTree != null)
                    m_ControlTree.RefreshControlValues();
            }
        }

        private static class Styles
        {
            public static string notFoundHelpText = "Device could not be found.";
        }

        private static class Contents
        {
            public static GUIContent clearContent = new GUIContent("Clear");
            public static GUIContent pauseContent = new GUIContent("Pause");
            public static GUIContent stateContent = new GUIContent("State");
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            AddToList();
        }
    }
}

#endif // UNITY_EDITOR
