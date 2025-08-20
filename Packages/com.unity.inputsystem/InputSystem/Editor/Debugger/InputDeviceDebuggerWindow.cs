#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

#if UNITY_6000_2_OR_NEWER
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

////TODO: allow selecting events and saving out only the selected ones

////TODO: add the ability for the debugger to just generate input on the device according to the controls it finds; good for testing

////TODO: add commands to event trace (also clickable)

////TODO: add diff-to-previous-event ability to event window

////FIXME: the repaint triggered from IInputStateCallbackReceiver somehow comes with a significant delay

////TODO: Add "Remote:" field in list that also has a button for local devices that allows to mirror them and their input
////      into connected players

////TODO: this window should help diagnose problems in the event stream (e.g. ignored state events and why they were ignored)

////TODO: add toggle to that switches to displaying raw control values

////TODO: allow adding visualizers (or automatically add them in cases) to control that show value over time (using InputStateHistory)

////TODO: show default states of controls

////TODO: provide ability to save and load event traces; also ability to record directly to a file
////TODO: provide ability to scrub back and forth through history

namespace UnityEngine.InputSystem.Editor
{
    // Shows status and activity of a single input device in a separate window.
    // Can also be used to alter the state of a device by making up state events.
    internal sealed class InputDeviceDebuggerWindow : EditorWindow, ISerializationCallbackReceiver, IDisposable
    {
        // ATM the debugger window is super slow and repaints are very expensive. So keep the total
        // number of events we can fit at a relatively low size until we have fixed that problem.
        private const int kDefaultEventTraceSizeInKB = 512;
        private const int kMaxEventsPerTrace = 1024;

        internal static InlinedArray<Action<InputDevice>> s_OnToolbarGUIActions;

        public static event Action<InputDevice> onToolbarGUI
        {
            add => s_OnToolbarGUIActions.Append(value);
            remove => s_OnToolbarGUIActions.Remove(value);
        }

        public static void CreateOrShowExisting(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            // See if we have an existing window for the device and if so pop it
            // in front.
            if (s_OpenDebuggerWindows != null)
            {
                for (var i = 0; i < s_OpenDebuggerWindows.Count; ++i)
                {
                    var existingWindow = s_OpenDebuggerWindows[i];
                    if (existingWindow.m_DeviceId == device.deviceId)
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

                InputSystem.onDeviceChange -= OnDeviceChange;
                InputState.onChange -= OnDeviceStateChange;
                InputSystem.onSettingsChange -= NeedControlValueRefresh;
                Application.focusChanged -= OnApplicationFocusChange;
                EditorApplication.playModeStateChanged += OnPlayModeChange;
                EditorApplication.update -= OnEditorUpdate;
            }

            m_EventTrace?.Dispose();
            m_EventTrace = null;

            m_ReplayController?.Dispose();
            m_ReplayController = null;
        }

        public void Dispose()
        {
            m_EventTrace?.Dispose();
            m_ReplayController?.Dispose();
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
            if (!string.IsNullOrEmpty(m_Device.description.interfaceName))
                EditorGUILayout.LabelField("Interface", m_Device.description.interfaceName);
            if (!string.IsNullOrEmpty(m_Device.description.product))
                EditorGUILayout.LabelField("Product", m_Device.description.product);
            if (!string.IsNullOrEmpty(m_Device.description.manufacturer))
                EditorGUILayout.LabelField("Manufacturer", m_Device.description.manufacturer);
            if (!string.IsNullOrEmpty(m_Device.description.version))
                EditorGUILayout.LabelField("Version", m_Device.description.version);
            if (!string.IsNullOrEmpty(m_Device.description.serial))
                EditorGUILayout.LabelField("Serial Number", m_Device.description.serial);
            EditorGUILayout.LabelField("Device ID", m_DeviceIdString);
            if (!string.IsNullOrEmpty(m_DeviceUsagesString))
                EditorGUILayout.LabelField("Usages", m_DeviceUsagesString);
            if (!string.IsNullOrEmpty(m_DeviceFlagsString))
                EditorGUILayout.LabelField("Flags", m_DeviceFlagsString);
            if (m_Device is Keyboard)
                EditorGUILayout.LabelField("Keyboard Layout", ((Keyboard)m_Device).keyboardLayout);
            const string sampleFrequencyTooltip = "Displays the current event or sample frequency of this device in Hertz (Hz) averaged over measurement period of 1 second. " +
                "The target frequency is device and backend dependent and may not be supported by all devices nor backends. " +
                "The Polling Frequency indicates system polling target frequency.";
            if (!string.IsNullOrEmpty(m_DeviceFrequencyString))
                EditorGUILayout.LabelField(new GUIContent("Sample Frequency", sampleFrequencyTooltip), new GUIContent(m_DeviceFrequencyString), EditorStyles.label);
            const string processingDelayTooltip =
                "Displays the average, minimum and maximum observed input processing delay. This shows the time from " +
                "when an input event is first created within Unity until its processed by the Input System. " +
                "Note that this excludes additional input latency introduced by OS, driver or device communication. " +
                "It also doesn't include output latency introduced by script processing, rendering, swap-chains, display refresh latency etc.";
            if (!string.IsNullOrEmpty(m_DeviceLatencyString))
                EditorGUILayout.LabelField(new GUIContent("Processing Delay", processingDelayTooltip),
                    new GUIContent(m_DeviceLatencyString), EditorStyles.label);
            EditorGUILayout.EndVertical();

            DrawControlTree();
            DrawEventList();
        }

        private void DrawControlTree()
        {
            var label = m_InputUpdateTypeShownInControlTree == InputUpdateType.Editor
                ? Contents.editorStateContent
                : Contents.playerStateContent;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(label, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
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

            if (m_NeedControlValueRefresh)
            {
                RefreshControlTreeValues();
                m_NeedControlValueRefresh = false;
            }

            if (m_Device.disabledInFrontend)
                EditorGUILayout.HelpBox("Device is DISABLED. Control values will not receive updates. "
                    + "To force-enable the device, you can right-click it in the input debugger and use 'Enable Device'.", MessageType.Info);

            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_ControlTree.OnGUI(rect);
        }

        private void DrawEventList()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Events", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            if (m_ReplayController != null && !m_ReplayController.finished)
                EditorGUILayout.LabelField("Playing...", EditorStyles.miniLabel);

            // Text field to determine size of event trace.
            var currentTraceSizeInKb = m_EventTrace.allocatedSizeInBytes / 1024;
            var oldSizeText = currentTraceSizeInKb + " KB";
            var newSizeText = EditorGUILayout.DelayedTextField(oldSizeText, Styles.toolbarTextField, GUILayout.Width(75));
            if (oldSizeText != newSizeText && StringHelpers.FromNicifiedMemorySize(newSizeText, out var newSizeInBytes, defaultMultiplier: 1024))
                m_EventTrace.Resize(newSizeInBytes);

            // Button to clear event trace.
            if (GUILayout.Button(Contents.clearContent, Styles.toolbarButton))
            {
                m_EventTrace.Clear();
                m_EventTree.Reload();
            }

            // Button to disable event tracing.
            // NOTE: We force-disable event tracing while a replay is in progress.
            using (new EditorGUI.DisabledScope(m_ReplayController != null && !m_ReplayController.finished))
            {
                var eventTraceDisabledNow = GUILayout.Toggle(!m_EventTraceDisabled, Contents.pauseContent, Styles.toolbarButton);
                if (eventTraceDisabledNow != m_EventTraceDisabled)
                {
                    m_EventTraceDisabled = eventTraceDisabledNow;
                    if (eventTraceDisabledNow)
                        m_EventTrace.Disable();
                    else
                        m_EventTrace.Enable();
                }
            }

            // Button to toggle recording of frame markers.
            m_EventTrace.recordFrameMarkers =
                GUILayout.Toggle(m_EventTrace.recordFrameMarkers, Contents.recordFramesContent, Styles.toolbarButton);

            // Button to save event trace to file.
            if (GUILayout.Button(Contents.saveContent, Styles.toolbarButton))
            {
                var defaultName = m_Device?.displayName + ".inputtrace";
                var fileName = EditorUtility.SaveFilePanel("Choose where to save event trace", string.Empty, defaultName, "inputtrace");
                if (!string.IsNullOrEmpty(fileName))
                    m_EventTrace.WriteTo(fileName);
            }

            // Button to load event trace from file.
            if (GUILayout.Button(Contents.loadContent, Styles.toolbarButton))
            {
                var fileName = EditorUtility.OpenFilePanel("Choose event trace to load", string.Empty, "inputtrace");
                if (!string.IsNullOrEmpty(fileName))
                {
                    // If replay is in progress, stop it.
                    if (m_ReplayController != null)
                    {
                        m_ReplayController.Dispose();
                        m_ReplayController = null;
                    }

                    // Make sure event trace isn't recording while we're playing.
                    m_EventTrace.Disable();
                    m_EventTraceDisabled = true;

                    m_EventTrace.ReadFrom(fileName);
                    m_EventTree.Reload();

                    m_ReplayController = m_EventTrace.Replay()
                        .PlayAllFramesOneByOne()
                        .OnFinished(() =>
                        {
                            m_ReplayController.Dispose();
                            m_ReplayController = null;
                            Repaint();
                        });
                }
            }

            GUILayout.EndHorizontal();

            if (m_ReloadEventTree)
            {
                m_ReloadEventTree = false;
                m_EventTree.Reload();
            }

            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_EventTree.OnGUI(rect);
        }

        ////FIXME: some of the state in here doesn't get refreshed when it's changed on the device
        private void InitializeWith(InputDevice device)
        {
            m_Device = device;
            m_DeviceId = device.deviceId;
            m_DeviceIdString = device.deviceId.ToString();
            m_DeviceUsagesString = string.Join(", ", device.usages.Select(x => x.ToString()).ToArray());

            UpdateDeviceFlags();

            // Query the sampling frequency of the device.
            // We do this synchronously here for simplicity.
            var queryFrequency = QuerySamplingFrequencyCommand.Create();
            var result = device.ExecuteCommand(ref queryFrequency);
            var targetFrequency = float.NaN;
            if (result >= 0)
                targetFrequency = queryFrequency.frequency;
            var realtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
            m_SampleFrequencyCalculator = new SampleFrequencyCalculator(targetFrequency, realtimeSinceStartup);
            m_InputLatencyCalculator = new InputLatencyCalculator(realtimeSinceStartup);

            // Set up event trace. The default trace size of 512kb fits a ton of events and will
            // likely bog down the UI if we try to display that many events. Instead, come up
            // with a more reasonable sized based on the state size of the device.
            if (m_EventTrace == null)
            {
                var deviceStateSize = (int)device.stateBlock.alignedSizeInBytes;
                var traceSizeInBytes = (kDefaultEventTraceSizeInKB * 1024).AlignToMultipleOf(deviceStateSize);
                if (traceSizeInBytes / deviceStateSize > kMaxEventsPerTrace)
                    traceSizeInBytes = kMaxEventsPerTrace * deviceStateSize;

                m_EventTrace =
                    new InputEventTrace(traceSizeInBytes)
                {
                    deviceId = device.deviceId
                };
            }

            m_EventTrace.onEvent += _ => m_ReloadEventTree = true;
            if (!m_EventTraceDisabled)
                m_EventTrace.Enable();

            // Set up event tree.
            m_EventTree = InputEventTreeView.Create(m_Device, m_EventTrace, ref m_EventTreeState, ref m_EventTreeHeaderState);

            // Set up control tree.
            m_ControlTree = InputControlTreeView.Create(m_Device, 1, ref m_ControlTreeState, ref m_ControlTreeHeaderState);
            m_ControlTree.Reload();
            m_ControlTree.ExpandAll();

            AddToList();

            InputSystem.onSettingsChange += NeedControlValueRefresh;
            InputSystem.onDeviceChange += OnDeviceChange;
            InputState.onChange += OnDeviceStateChange;
            Application.focusChanged += OnApplicationFocusChange;
            EditorApplication.playModeStateChanged += OnPlayModeChange;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            StringBuilder sb = null;
            bool needControlValueRefresh = false;
            var realtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
            if (m_SampleFrequencyCalculator.Update(realtimeSinceStartup))
            {
                m_DeviceFrequencyString = CreateDeviceFrequencyString(ref sb);
                needControlValueRefresh = true;
            }
            if (m_InputLatencyCalculator.Update(realtimeSinceStartup))
            {
                m_DeviceLatencyString = CreateDeviceLatencyString(ref sb);
                needControlValueRefresh = true;
            }
            if (needControlValueRefresh)
                NeedControlValueRefresh();
        }

        private string CreateDeviceFrequencyString(ref StringBuilder sb)
        {
            if (sb == null)
                sb = new StringBuilder();
            else
                sb.Clear();

            // Display achievable frequency for device
            const string frequencyFormat = "0.000 Hz";
            sb.Append(m_SampleFrequencyCalculator.frequency.ToString(frequencyFormat, CultureInfo.InvariantCulture));

            // Display target frequency reported for device
            sb.Append(" (Target @ ");
            sb.Append(float.IsNaN(m_SampleFrequencyCalculator.targetFrequency)
                ? "n/a"
                : m_SampleFrequencyCalculator.targetFrequency.ToString(frequencyFormat));

            // Display system-wide polling frequency
            sb.Append(", Polling-Frequency @ ");
            sb.Append(InputSystem.pollingFrequency.ToString(frequencyFormat));
            sb.Append(')');

            return sb.ToString();
        }

        private static void FormatLatency(StringBuilder sb, float value)
        {
            const string latencyFormat = "0.000 ms";
            if (float.IsNaN(value))
            {
                sb.Append("n/a");
                return;
            }

            var millis = 1000.0f * value;
            sb.Append(millis <= 1000.0f
                ? (millis).ToString(latencyFormat, CultureInfo.InvariantCulture)
                : ">1000.0 ms");
        }

        private string CreateDeviceLatencyString(ref StringBuilder sb)
        {
            if (sb == null)
                sb = new StringBuilder();
            else
                sb.Clear();

            // Display latency in seconds for device
            sb.Append("Average: ");
            FormatLatency(sb, m_InputLatencyCalculator.averageLatencySeconds);
            sb.Append(", Min: ");
            FormatLatency(sb, m_InputLatencyCalculator.minLatencySeconds);
            sb.Append(", Max: ");
            FormatLatency(sb, m_InputLatencyCalculator.maxLatencySeconds);

            return sb.ToString();
        }

        private void UpdateDeviceFlags()
        {
            var flags = new List<string>();
            if (m_Device.native)
                flags.Add("Native");
            if (m_Device.remote)
                flags.Add("Remote");
            if (m_Device.updateBeforeRender)
                flags.Add("UpdateBeforeRender");
            if (m_Device.hasStateCallbacks)
                flags.Add("HasStateCallbacks");
            if (m_Device.hasEventMerger)
                flags.Add("HasEventMerger");
            if (m_Device.hasEventPreProcessor)
                flags.Add("HasEventPreProcessor");
            if (m_Device.disabledInFrontend)
                flags.Add("DisabledInFrontend");
            if (m_Device.disabledInRuntime)
                flags.Add("DisabledInRuntime");
            if (m_Device.disabledWhileInBackground)
                flags.Add("DisabledWhileInBackground");
            if (m_Device.canDeviceRunInBackground)
                flags.Add("CanRunInBackground");
            m_DeviceFlags = m_Device.m_DeviceFlags;
            m_DeviceFlagsString = string.Join(", ", flags.ToArray());
        }

        private void RefreshControlTreeValues()
        {
            m_InputUpdateTypeShownInControlTree = DetermineUpdateTypeToShow(m_Device);
            var currentUpdateType = InputState.currentUpdateType;

            InputStateBuffers.SwitchTo(InputSystem.s_Manager.m_StateBuffers, m_InputUpdateTypeShownInControlTree);
            m_ControlTree.RefreshControlValues();
            InputStateBuffers.SwitchTo(InputSystem.s_Manager.m_StateBuffers, currentUpdateType);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "device", Justification = "Keep this for future implementation")]
        internal static InputUpdateType DetermineUpdateTypeToShow(InputDevice device)
        {
            if (EditorApplication.isPlaying)
            {
                // In play mode, while playing, we show player state. Period.

                switch (InputSystem.settings.updateMode)
                {
                    case InputSettings.UpdateMode.ProcessEventsManually:
                        return InputUpdateType.Manual;

                    case InputSettings.UpdateMode.ProcessEventsInFixedUpdate:
                        return InputUpdateType.Fixed;

                    default:
                        return InputUpdateType.Dynamic;
                }
            }

            // Outside of play mode, always show editor state.
            return InputUpdateType.Editor;
        }

        // We will lose our device on domain reload and then look it back up the first
        // time we hit a repaint after a reload. By that time, the input system should have
        // fully come back to life as well.
        private InputDevice m_Device;
        private string m_DeviceIdString;
        private string m_DeviceUsagesString;
        private string m_DeviceFlagsString;
        private string m_DeviceFrequencyString;
        private string m_DeviceLatencyString;
        private InputDevice.DeviceFlags m_DeviceFlags;
        private InputControlTreeView m_ControlTree;
        private InputEventTreeView m_EventTree;
        private bool m_NeedControlValueRefresh;
        private bool m_ReloadEventTree;
        private InputEventTrace.ReplayController m_ReplayController;
        private InputEventTrace m_EventTrace;
        private InputUpdateType m_InputUpdateTypeShownInControlTree;
        private InputLatencyCalculator m_InputLatencyCalculator;
        private SampleFrequencyCalculator m_SampleFrequencyCalculator;

        [SerializeField] private int m_DeviceId = InputDevice.InvalidDeviceId;
        [SerializeField] private TreeViewState m_ControlTreeState;
        [SerializeField] private TreeViewState m_EventTreeState;
        [SerializeField] private MultiColumnHeaderState m_ControlTreeHeaderState;
        [SerializeField] private MultiColumnHeaderState m_EventTreeHeaderState;
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
            s_OpenDebuggerWindows?.Remove(this);
        }

        private void NeedControlValueRefresh()
        {
            m_NeedControlValueRefresh = true;
            Repaint();
        }

        private void OnPlayModeChange(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode || change == PlayModeStateChange.EnteredEditMode)
                NeedControlValueRefresh();
        }

        private void OnApplicationFocusChange(bool focus)
        {
            NeedControlValueRefresh();
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device.deviceId != m_DeviceId)
                return;

            if (change == InputDeviceChange.Removed)
            {
                Close();
            }
            else
            {
                if (m_DeviceFlags != device.m_DeviceFlags)
                    UpdateDeviceFlags();
                Repaint();
            }
        }

        private void OnDeviceStateChange(InputDevice device, InputEventPtr eventPtr)
        {
            if (device == m_Device)
            {
                m_InputLatencyCalculator.ProcessSample(eventPtr);
                m_SampleFrequencyCalculator.ProcessSample(eventPtr);

                NeedControlValueRefresh();
            }
        }

        private static class Styles
        {
            public static string notFoundHelpText = "Device could not be found.";

            public static GUIStyle toolbarTextField;
            public static GUIStyle toolbarButton;

            static Styles()
            {
                toolbarTextField = new GUIStyle(EditorStyles.toolbarTextField);
                toolbarTextField.alignment = TextAnchor.MiddleRight;

                toolbarButton = new GUIStyle(EditorStyles.toolbarButton);
                toolbarButton.alignment = TextAnchor.MiddleCenter;
            }
        }

        private static class Contents
        {
            public static GUIContent clearContent = new GUIContent("Clear");
            public static GUIContent pauseContent = new GUIContent("Pause");
            public static GUIContent saveContent = new GUIContent("Save");
            public static GUIContent loadContent = new GUIContent("Load");
            public static GUIContent recordFramesContent = new GUIContent("Record Frames");
            public static GUIContent stateContent = new GUIContent("State");
            public static GUIContent editorStateContent = new GUIContent("Controls (Editor State)");
            public static GUIContent playerStateContent = new GUIContent("Controls (Player State)");
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
