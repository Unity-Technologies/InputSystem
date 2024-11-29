using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

////TODO: add way to plot values over time

// Goal is to build this out into something that can visualize a large number of
// aspects about an InputControl/InputDevice especially with an eye towards making
// it a good deal to debug any input collection/processing irregularities that may
// be seen in players (or the editor, for that matter).

// Some fields assigned through only through serialization.
#pragma warning disable CS0649

namespace UnityEngine.InputSystem.Samples
{
    /// <summary>
    /// A component for debugging purposes that adds an on-screen display which shows
    /// activity on an input control over time.
    /// </summary>
    /// <remarks>
    /// This component is most useful for debugging input directly on the source device.
    /// </remarks>
    /// <seealso cref="InputActionVisualizer"/>
    [AddComponentMenu("Input/Debug/Input Control Visualizer")]
    [ExecuteInEditMode]
    public class InputControlVisualizer : InputVisualizer
    {
        /// <summary>
        /// What kind of visualization to show.
        /// </summary>
        public Mode visualization
        {
            get => m_Visualization;
            set
            {
                if (m_Visualization == value)
                    return;
                m_Visualization = value;
                SetupVisualizer();
            }
        }

        /// <summary>
        /// Path of the control that is to be visualized.
        /// </summary>
        /// <seealso cref="InputControlPath"/>
        /// <seealso cref="InputControl.path"/>
        public string controlPath
        {
            get => m_ControlPath;
            set
            {
                m_ControlPath = value;
                if (m_Control != null)
                    ResolveControl();
            }
        }

        /// <summary>
        /// If, at runtime, multiple controls are matching <see cref="controlPath"/>, this property
        /// determines the index of the control that is retrieved from the possible options.
        /// </summary>
        public int controlIndex
        {
            get => m_ControlIndex;
            set
            {
                m_ControlIndex = value;
                if (m_Control != null)
                    ResolveControl();
            }
        }

        /// <summary>
        /// The control resolved from <see cref="controlPath"/> at runtime. May be null.
        /// </summary>
        public InputControl control => m_Control;

        protected new void OnEnable()
        {
            if (m_Visualization == Mode.None)
                return;

            if (s_EnabledInstances == null)
                s_EnabledInstances = new List<InputControlVisualizer>();
            if (s_EnabledInstances.Count == 0)
            {
                InputSystem.onDeviceChange += OnDeviceChange;
                InputSystem.onEvent += OnEvent;
            }
            s_EnabledInstances.Add(this);

            ResolveControl();

            base.OnEnable();
        }

        protected new void OnDisable()
        {
            if (m_Visualization == Mode.None)
                return;

            if (s_EnabledInstances != null)
            {
                s_EnabledInstances.Remove(this);
                if (s_EnabledInstances.Count == 0)
                {
                    InputSystem.onDeviceChange -= OnDeviceChange;
                    InputSystem.onEvent -= OnEvent;
                }
            }

            m_Control = null;

            base.OnDisable();
        }

        protected new void OnGUI()
        {
            if (m_Visualization == Mode.None)
                return;

            base.OnGUI();
        }

        protected new void OnValidate()
        {
            ResolveControl();
            base.OnValidate();
        }

        [Tooltip("The type of visualization to perform for the control.")]
        [SerializeField] private Mode m_Visualization;
        [Tooltip("Path of the control that should be visualized. If at runtime, multiple "
            + "controls match the given path, the 'Control Index' property can be used to decide "
            + "which of the controls to visualize.")]
        [InputControl, SerializeField] private string m_ControlPath;
        [Tooltip("If multiple controls match 'Control Path' at runtime, this property decides "
            + "which control to visualize from the list of candidates. It is a zero-based index. " +
            "This is ignored if using current device instead.")]
        [SerializeField] private int m_ControlIndex;

        [Tooltip("If set, ignores control index and maps a control of the current device (if it exist) or none.")]
        [SerializeField] private bool m_UseCurrentDevice;

        [NonSerialized] private InputControl m_Control;

        private static List<InputControlVisualizer> s_EnabledInstances;

        private static InputControl ResolveCurrentControl(InputControlList<InputControl> candidates)
        {
            // Only accept control that belongs to the current device of the same device type as candidate control device type.
            foreach (var candidate in candidates)
            {
                var currentDevice = GetCurrentDevice(candidate.device);
                if (candidate.device == currentDevice)
                    return candidate;
            }

            return null;
        }

        private void ResolveControl()
        {
            m_Control = null;
            if (string.IsNullOrEmpty(m_ControlPath))
                return;

            using (var candidates = InputSystem.FindControls(m_ControlPath))
            {
                var numCandidates = candidates.Count;
                if (m_UseCurrentDevice)
                    m_Control = ResolveCurrentControl(candidates);
                else if (numCandidates > 1 && m_ControlIndex < numCandidates && m_ControlIndex >= 0)
                    m_Control = candidates[m_ControlIndex];
                else if (numCandidates > 0)
                    m_Control = candidates[0];
            }

            SetupVisualizer();
        }

        void Update()
        {
            // There is currently no callback when current device changes so we will reattempt to resolve control
            if (m_UseCurrentDevice)
            {
                if (m_Control != null && m_Control.device != GetCurrentDevice(m_Control.device))
                    m_Control = null;
                if (m_Control == null)
                    ResolveControl();
            }
        }

        private static InputDevice GetCurrentDevice(InputDevice device)
        {
            if (device is Gamepad) return Gamepad.current;
            if (device is Mouse) return Mouse.current;
            if (device is Pen) return Pen.current;
            if (device is Pointer) return Pointer.current; // should be last, because it's a base class for Mouse and Pen

            throw new ArgumentException(
                $"Expected device type that implements .current, but got '{device.name}' (deviceId: {device.deviceId}) instead ");
        }

        private static VisualizationHelpers.Visualizer CreateVisualizer(Mode mode, InputControl control, int historySamples)
        {
            switch (mode)
            {
                case Mode.Value:
                {
                    // This visualization mode requires a control
                    if (control == null)
                        return null;

                    VisualizationHelpers.Visualizer visualizer = null;
                    var valueType = control.valueType;
                    if (valueType == typeof(Vector2))
                        visualizer = new VisualizationHelpers.Vector2Visualizer(historySamples);
                    else if (valueType == typeof(float))
                        visualizer = new VisualizationHelpers.ScalarVisualizer<float>(historySamples)
                        {
                            ////TODO: pass actual min/max limits of control
                            limitMax = 1,
                            limitMin = 0
                        };
                    else if (valueType == typeof(int))
                        visualizer = new VisualizationHelpers.ScalarVisualizer<int>(historySamples)
                        {
                            ////TODO: pass actual min/max limits of control
                            limitMax = 1,
                            limitMin = 0
                        };
                    else
                    {
                        ////TODO: generic visualizer
                    }
                    return visualizer;
                }

                case Mode.Events:
                {
                    var visualizer = new VisualizationHelpers.TimelineVisualizer(historySamples)
                    {
                        timeUnit = VisualizationHelpers.TimelineVisualizer.TimeUnit.Frames,
                        historyDepth = historySamples,
                        showLimits = true,
                        limitsY = new Vector2(0, 5) // Will expand upward automatically
                    };
                    visualizer.AddTimeline("Events", Color.green,
                        VisualizationHelpers.TimelineVisualizer.PlotType.BarChart);
                    return visualizer;
                }

                case Mode.MaximumLag:
                {
                    var visualizer = new VisualizationHelpers.TimelineVisualizer(historySamples)
                    {
                        timeUnit = VisualizationHelpers.TimelineVisualizer.TimeUnit.Frames,
                        historyDepth = historySamples,
                        valueUnit = new GUIContent("ms"),
                        showLimits = true,
                        limitsY = new Vector2(0, 6)
                    };
                    visualizer.AddTimeline("MaxLag", Color.red,
                        VisualizationHelpers.TimelineVisualizer.PlotType.BarChart);
                    return visualizer;
                }

                case Mode.Bytes:
                {
                    var visualizer = new VisualizationHelpers.TimelineVisualizer(historySamples)
                    {
                        timeUnit = VisualizationHelpers.TimelineVisualizer.TimeUnit.Frames,
                        valueUnit = new GUIContent("bytes"),
                        historyDepth = historySamples,
                        showLimits = true,
                        limitsY = new Vector2(0, 64)
                    };
                    visualizer.AddTimeline("Bytes", Color.red,
                        VisualizationHelpers.TimelineVisualizer.PlotType.BarChart);
                    return visualizer;
                }

                case Mode.DeviceCurrent:
                    return new VisualizationHelpers.CurrentDeviceVisualizer();

                default:
                    throw new ArgumentOutOfRangeException(mode.ToString());
            }
        }

        private void SetupVisualizer()
        {
            m_Visualizer = CreateVisualizer(m_Visualization, m_Control, m_HistorySamples);
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change != InputDeviceChange.Added && change != InputDeviceChange.Removed)
                return;

            for (var i = 0; i < s_EnabledInstances.Count; ++i)
            {
                var component = s_EnabledInstances[i];
                if (change == InputDeviceChange.Removed && component.m_Control != null &&
                    component.m_Control.device == device)
                    component.ResolveControl();
                else if (change == InputDeviceChange.Added)
                    component.ResolveControl();
            }
        }

        private static void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            // Ignore very first update as we usually get huge lag spikes and event count
            // spikes in it from stuff that has accumulated while going into play mode or
            // starting up the player.
            if (InputState.updateCount <= 1)
                return;

            if (InputState.currentUpdateType == InputUpdateType.Editor)
                return;

            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;

            for (var i = 0; i < s_EnabledInstances.Count; ++i)
            {
                var component = s_EnabledInstances[i];
                if (component.m_Control?.device != device || component.m_Visualizer == null)
                    continue;

                component.OnEventImpl(eventPtr, device);
            }
        }

        private unsafe void OnEventImpl(InputEventPtr eventPtr, InputDevice device)
        {
            switch (m_Visualization)
            {
                case Mode.Value:
                {
                    var statePtr = m_Control.GetStatePtrFromStateEvent(eventPtr);
                    if (statePtr == null)
                        return; // No value for control in event.
                    var value = m_Control.ReadValueFromStateAsObject(statePtr);
                    m_Visualizer.AddSample(value, eventPtr.time);
                    break;
                }

                case Mode.Events:
                {
                    var visualizer = (VisualizationHelpers.TimelineVisualizer)m_Visualizer;
                    var frame = (int)InputState.updateCount;
                    ref var valueRef = ref visualizer.GetOrCreateSample(0, frame);
                    var value = valueRef.ToInt32() + 1;
                    valueRef = value;
                    visualizer.limitsY =
                        new Vector2(0, Mathf.Max(value, visualizer.limitsY.y));
                    break;
                }

                case Mode.MaximumLag:
                {
                    var visualizer = (VisualizationHelpers.TimelineVisualizer)m_Visualizer;
                    var lag = (Time.realtimeSinceStartup - eventPtr.time) * 1000; // In milliseconds.
                    var frame = (int)InputState.updateCount;
                    ref var valueRef = ref visualizer.GetOrCreateSample(0, frame);

                    if (lag > valueRef.ToDouble())
                    {
                        valueRef = lag;
                        if (lag > visualizer.limitsY.y)
                            visualizer.limitsY = new Vector2(0, Mathf.Ceil((float)lag));
                    }
                    break;
                }

                case Mode.Bytes:
                {
                    var visualizer = (VisualizationHelpers.TimelineVisualizer)m_Visualizer;
                    var frame = (int)InputState.updateCount;
                    ref var valueRef = ref visualizer.GetOrCreateSample(0, frame);
                    var value = valueRef.ToInt32() + eventPtr.sizeInBytes;
                    valueRef = value;
                    visualizer.limitsY =
                        new Vector2(0, Mathf.Max(value, visualizer.limitsY.y));
                    break;
                }

                case Mode.DeviceCurrent:
                {
                    m_Visualizer.AddSample(device, eventPtr.time);
                    break;
                }
            }
        }

        /// <summary>
        /// Determines which aspect of the control should be visualized.
        /// </summary>
        public enum Mode
        {
            None = 0,
            Value = 1,
            Events = 4,
            MaximumLag = 6,
            Bytes = 7,
            DeviceCurrent = 8,
        }
    }
}
