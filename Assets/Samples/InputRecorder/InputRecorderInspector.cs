#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Events;

////TODO: add ability to inspect contents of event traces in a separate window

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A custom inspector for <see cref="InputRecorder"/>. Adds UI elements to store captures in files, to load them from
    /// there, and to initiate replays from within the editor. It also shows information for when captures or replays are
    /// in progress.
    /// </summary>
    [CustomEditor(typeof(InputRecorder))]
    internal class InputRecorderInspector : UnityEditor.Editor
    {
        protected void OnEnable()
        {
            m_DevicePathProperty = serializedObject.FindProperty("m_DevicePath");
            m_RecordButtonPath = serializedObject.FindProperty("m_RecordButtonPath");
            m_PlayButtonPathProperty = serializedObject.FindProperty("m_PlayButtonPath");
            m_RecordFramesProperty = serializedObject.FindProperty("m_RecordFrames");
            m_RecordStateEventsOnlyProperty = serializedObject.FindProperty("m_RecordStateEventsOnly");
            m_ReplayOnNewDevicesProperty = serializedObject.FindProperty("m_ReplayOnNewDevices");
            m_SimulateTimingOnReplayProperty = serializedObject.FindProperty("m_SimulateOriginalTimingOnReplay");
            m_CaptureMemoryDefaultSizeProperty = serializedObject.FindProperty("m_CaptureMemoryDefaultSize");
            m_CaptureMemoryMaxSizeProperty = serializedObject.FindProperty("m_CaptureMemoryMaxSize");
            m_StartRecordingWhenEnabledProperty = serializedObject.FindProperty("m_StartRecordingWhenEnabled");

            m_AllInput = string.IsNullOrEmpty(m_DevicePathProperty.stringValue);

            m_PlayText = EditorGUIUtility.TrIconContent("PlayButton", "Play the current input capture.");
            m_PauseText = EditorGUIUtility.TrIconContent("PauseButton", "Pause the current input playback.");
            m_ResumeText = EditorGUIUtility.TrIconContent("PauseButton On", "Resume the current input playback.");
            m_StepForwardText = EditorGUIUtility.TrIconContent("d_StepButton", "Play the next input event.");
            m_StepBackwardText = EditorGUIUtility.TrIconContent("d_StepLeftButton", "Play the previous input event.");
            m_StopText = EditorGUIUtility.TrIconContent("PlayButton On", "Stop the current input playback.");
            m_RecordText = EditorGUIUtility.TrIconContent("Animation.Record", "Start recording input.");

            var recorder = (InputRecorder)serializedObject.targetObject;
            m_OnRecordEvent = _ => Repaint();
            recorder.changeEvent.AddListener(m_OnRecordEvent);
        }

        protected void OnDisable()
        {
            var recorder = (InputRecorder)serializedObject.targetObject;
            recorder.changeEvent.RemoveListener(m_OnRecordEvent);
        }

        public override void OnInspectorGUI()
        {
            var recorder = (InputRecorder)serializedObject.targetObject;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var newAllInput = EditorGUILayout.Toggle(m_AllInputText, m_AllInput);
                if (!newAllInput)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(m_DevicePathProperty, m_DeviceText);
                    }
                }
                else if (newAllInput != m_AllInput)
                {
                    m_DevicePathProperty.stringValue = string.Empty;
                }
                m_AllInput = newAllInput;

                EditorGUILayout.PropertyField(m_RecordFramesProperty);
                EditorGUILayout.PropertyField(m_RecordStateEventsOnlyProperty);
                EditorGUILayout.PropertyField(m_ReplayOnNewDevicesProperty);
                EditorGUILayout.PropertyField(m_SimulateTimingOnReplayProperty);
                EditorGUILayout.PropertyField(m_StartRecordingWhenEnabledProperty, m_RecordWhenEnabledText);

                var defaultSizeInMB = m_CaptureMemoryDefaultSizeProperty.intValue / (1024 * 1024);
                var newDefaultSizeInMB = EditorGUILayout.IntSlider(m_DefaultSizeText, defaultSizeInMB, 1, 100);
                if (newDefaultSizeInMB != defaultSizeInMB)
                    m_CaptureMemoryDefaultSizeProperty.intValue = newDefaultSizeInMB * 1024 * 1024;

                var maxSizeInMB = m_CaptureMemoryMaxSizeProperty.intValue / (1024 * 1024);
                var newMaxSizeInMB = maxSizeInMB < newDefaultSizeInMB
                    ? newDefaultSizeInMB
                    : EditorGUILayout.IntSlider(m_MaxSizeText, maxSizeInMB, 1, 100);
                if (newMaxSizeInMB != maxSizeInMB)
                    m_CaptureMemoryMaxSizeProperty.intValue = newMaxSizeInMB * 1024 * 1024;

                EditorGUILayout.PropertyField(m_RecordButtonPath, m_RecordButtonText);
                EditorGUILayout.PropertyField(m_PlayButtonPathProperty, m_PlayButtonText);

                if (scope.changed)
                    serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                ////TODO: go-to-next and go-to-previous button
                // Play and pause buttons.
                EditorGUI.BeginDisabledGroup(recorder.eventCount == 0 || recorder.captureIsRunning);
                var oldIsPlaying = recorder.replayIsRunning;
                var newIsPlaying = GUILayout.Toggle(oldIsPlaying, !oldIsPlaying ? m_PlayText : m_StopText, EditorStyles.miniButton,
                    GUILayout.Width(50));
                if (oldIsPlaying != newIsPlaying)
                {
                    if (newIsPlaying)
                        recorder.StartReplay();
                    else
                        recorder.StopReplay();
                }
                if (newIsPlaying && recorder.replay != null && GUILayout.Button(recorder.replay.paused ? m_ResumeText : m_PauseText, EditorStyles.miniButton,
                    GUILayout.Width(50)))
                {
                    if (recorder.replay.paused)
                        recorder.StartReplay();
                    else
                        recorder.PauseReplay();
                }
                EditorGUI.EndDisabledGroup();

                // Record button.
                EditorGUI.BeginDisabledGroup(recorder.replayIsRunning);
                var oldIsRecording = recorder.captureIsRunning;
                var newIsRecording = GUILayout.Toggle(oldIsRecording, m_RecordText, EditorStyles.miniButton, GUILayout.Width(50));
                if (oldIsRecording != newIsRecording)
                {
                    if (newIsRecording)
                        recorder.StartCapture();
                    else
                        recorder.StopCapture();
                }
                EditorGUI.EndDisabledGroup();

                // Load button.
                EditorGUI.BeginDisabledGroup(recorder.replayIsRunning);
                if (GUILayout.Button("Load"))
                {
                    var filePath = EditorUtility.OpenFilePanel("Choose Input Event Trace to Load", string.Empty, "inputtrace");
                    if (!string.IsNullOrEmpty(filePath))
                        recorder.LoadCaptureFromFile(filePath);
                }
                EditorGUI.EndDisabledGroup();

                // Save button.
                EditorGUI.BeginDisabledGroup(recorder.eventCount == 0 || recorder.replayIsRunning);
                if (GUILayout.Button("Save"))
                {
                    var filePath = EditorUtility.SaveFilePanel("Choose Where to Save Input Event Trace", string.Empty, $"{recorder.gameObject.name}.inputtrace", "inputtrace");
                    if (!string.IsNullOrEmpty(filePath))
                        recorder.SaveCaptureToFile(filePath);
                }

                // Clear button.
                if (GUILayout.Button("Clear"))
                {
                    recorder.ClearCapture();
                    Repaint();
                }
                EditorGUI.EndDisabledGroup();
            }

            ////TODO: allow hotscrubbing
            // Play bar.
            EditorGUILayout.IntSlider(recorder.replayPosition, 0, (int)recorder.eventCount);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope())
            {
                EditorGUILayout.LabelField(m_InfoText, EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField($"{recorder.eventCount} events", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"{recorder.totalEventSizeInBytes / 1024} kb captured", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"{recorder.allocatedSizeInBytes / 1024} kb allocated", EditorStyles.miniLabel);

                    if (recorder.capture != null)
                    {
                        var devices = recorder.capture.deviceInfos;
                        if (devices.Count > 0)
                        {
                            EditorGUILayout.LabelField(m_DevicesText, EditorStyles.miniBoldLabel);
                            using (new EditorGUI.IndentLevelScope())
                            {
                                foreach (var device in devices)
                                {
                                    EditorGUILayout.LabelField(device.layout, EditorStyles.miniLabel);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool m_AllInput;
        private SerializedProperty m_DevicePathProperty;
        private SerializedProperty m_RecordButtonPath;
        private SerializedProperty m_PlayButtonPathProperty;
        private SerializedProperty m_RecordFramesProperty;
        private SerializedProperty m_RecordStateEventsOnlyProperty;
        private SerializedProperty m_ReplayOnNewDevicesProperty;
        private SerializedProperty m_SimulateTimingOnReplayProperty;
        private SerializedProperty m_CaptureMemoryDefaultSizeProperty;
        private SerializedProperty m_CaptureMemoryMaxSizeProperty;
        private SerializedProperty m_StartRecordingWhenEnabledProperty;
        private UnityAction<InputRecorder.Change> m_OnRecordEvent;

        private GUIContent m_RecordButtonText = new GUIContent("Record Button", "If set, this button will start and stop capture in play mode.");
        private GUIContent m_PlayButtonText = new GUIContent("Play Button", "If set, this button will start and stop replay of the current capture in play mode.");
        private GUIContent m_RecordWhenEnabledText = new GUIContent("Capture When Enabled", "If true, recording will start immediately when the component is enabled in play mode.");
        private GUIContent m_DevicesText = new GUIContent("Devices");
        private GUIContent m_AllInputText = new GUIContent("All Input", "Whether to record input from all devices or from just specific devices.");
        private GUIContent m_DeviceText = new GUIContent("Device", "Type of device to record input from.");
        private GUIContent m_InfoText = new GUIContent("Info:");
        private GUIContent m_DefaultSizeText = new GUIContent("Default Size (MB)", "Memory allocate for capture by default. Will automatically grow up to max memory.");
        private GUIContent m_MaxSizeText = new GUIContent("Max Size (MB)", "Maximum memory allocated for capture. Once a capture reaches this limit, new events will start overwriting old ones.");
        private GUIContent m_PlayText;
        private GUIContent m_PauseText;
        private GUIContent m_ResumeText;
        private GUIContent m_StepForwardText;
        private GUIContent m_StepBackwardText;
        private GUIContent m_StopText;
        private GUIContent m_RecordText;
    }
}
#endif
