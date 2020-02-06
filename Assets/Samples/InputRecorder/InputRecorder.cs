using System;
using UnityEngine.Events;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

////TODO: allow multiple device paths

////TODO: streaming support

////REVIEW: consider this for inclusion directly in the input system

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A wrapper component around <see cref="InputEventTrace"/> that provides an easy interface for recording input
    /// from a GameObject.
    /// </summary>
    /// <remarks>
    /// This component comes with a custom inspector that provides an easy recording and playback interface and also
    /// gives feedback about what has been recorded in the trace. The interface also allows saving and loading event
    /// traces.
    ///
    /// Capturing can either be constrained by a <see cref="devicePath"/> or capture all input occuring in the system.
    ///
    /// Replay by default will happen frame by frame (see <see cref="InputEventTrace.ReplayController.PlayAllFramesOneByOne"/>).
    /// If frame markers are disabled (see <see cref="recordFrames"/>), all events are queued right away in the first
    /// frame and replay completes immediately.
    ///
    /// Other than frame-by-frame, replay can be made to happen in a way that tries to simulate the original input
    /// timing. To do so, enable <see cref="simulateOriginalTimingOnReplay"/>. This will make use of <see
    /// cref="InputEventTrace.ReplayController.PlayAllEventsAccordingToTimestamps"/>
    /// </remarks>
    public class InputRecorder : MonoBehaviour
    {
        /// <summary>
        /// Whether a capture is currently in progress.
        /// </summary>
        /// <value>True if a capture is in progress.</value>
        public bool captureIsRunning => m_EventTrace != null && m_EventTrace.enabled;

        /// <summary>
        /// Whether a replay is currently being run by the component.
        /// </summary>
        /// <value>True if replay is running.</value>
        /// <seealso cref="replay"/>
        /// <seealso cref="StartReplay"/>
        /// <seealso cref="StopReplay"/>
        public bool replayIsRunning => m_ReplayController != null && !m_ReplayController.finished;

        /// <summary>
        /// If true, input recording is started immediately when the component is enabled. Disabled by default.
        /// Call <see cref="StartCapture"/> to manually start capturing.
        /// </summary>
        /// <value>True if component will start recording automatically in <see cref="OnEnable"/>.</value>
        /// <seealso cref="StartCapture"/>
        public bool startRecordingWhenEnabled
        {
            get => m_StartRecordingWhenEnabled;
            set
            {
                m_StartRecordingWhenEnabled = value;
                if (value && enabled && !captureIsRunning)
                    StartCapture();
            }
        }

        /// <summary>
        /// Total number of events captured.
        /// </summary>
        /// <value>Number of captured events.</value>
        public long eventCount => m_EventTrace?.eventCount ?? 0;

        /// <summary>
        /// Total size of captured events.
        /// </summary>
        /// <value>Size of captured events in bytes.</value>
        public long totalEventSizeInBytes => m_EventTrace?.totalEventSizeInBytes ?? 0;

        /// <summary>
        /// Total size of capture memory currently allocated.
        /// </summary>
        /// <value>Size of memory allocated for capture.</value>
        public long allocatedSizeInBytes => m_EventTrace?.allocatedSizeInBytes ?? 0;

        /// <summary>
        /// Whether to record frame marker events when capturing input. Enabled by default.
        /// </summary>
        /// <value>True if frame marker events will be recorded.</value>
        /// <seealso cref="InputEventTrace.recordFrameMarkers"/>
        public bool recordFrames
        {
            get => m_RecordFrames;
            set
            {
                if (m_RecordFrames == value)
                    return;
                m_RecordFrames = value;
                if (m_EventTrace != null)
                    m_EventTrace.recordFrameMarkers = m_RecordFrames;
            }
        }

        /// <summary>
        /// Whether to record only <see cref="StateEvent"/>s and <see cref="DeltaStateEvent"/>s. Disabled by
        /// default.
        /// </summary>
        /// <value>True if anything but state events should be ignored.</value>
        public bool recordStateEventsOnly
        {
            get => m_RecordStateEventsOnly;
            set => m_RecordStateEventsOnly = value;
        }

        /// <summary>
        /// Path that constrains the devices to record from.
        /// </summary>
        /// <value>Input control path to match devices or null/empty.</value>
        /// <remarks>
        /// By default, this is not set. Meaning that input will be recorded from all devices. By setting this property
        /// to a path, only events for devices that match the given path (as dictated by <see cref="InputControlPath.Matches"/>)
        /// will be recorded from.
        ///
        /// By setting this property to the exact path of a device at runtime, recording can be restricted to just that
        /// device.
        /// </remarks>
        /// <seealso cref="InputControlPath"/>
        /// <seealso cref="InputControlPath.Matches"/>
        public string devicePath
        {
            get => m_DevicePath;
            set => m_DevicePath = value;
        }

        public string recordButtonPath
        {
            get => m_RecordButtonPath;
            set
            {
                m_RecordButtonPath = value;
                HookOnInputEvent();
            }
        }

        public string playButtonPath
        {
            get => m_PlayButtonPath;
            set
            {
                m_PlayButtonPath = value;
                HookOnInputEvent();
            }
        }

        /// <summary>
        /// The underlying event trace that contains the captured input events.
        /// </summary>
        /// <value>Underlying event trace.</value>
        /// <remarks>
        /// This will be null if no capture is currently associated with the recorder.
        /// </remarks>
        public InputEventTrace capture => m_EventTrace;

        /// <summary>
        /// The replay controller for when a replay is running.
        /// </summary>
        /// <value>Replay controller for the event trace while replay is running.</value>
        /// <seealso cref="replayIsRunning"/>
        /// <seealso cref="StartReplay"/>
        public InputEventTrace.ReplayController replay => m_ReplayController;

        public int replayPosition
        {
            get
            {
                if (m_ReplayController != null)
                    return m_ReplayController.position;
                return 0;
            }
            ////TODO: allow setting replay position
        }

        /// <summary>
        /// Whether a replay should create new devices or replay recorded events as is. Disabled by default.
        /// </summary>
        /// <value>True if replay should temporary create new devices.</value>
        /// <seealso cref="InputEventTrace.ReplayController.WithAllDevicesMappedToNewInstances"/>
        public bool replayOnNewDevices
        {
            get => m_ReplayOnNewDevices;
            set => m_ReplayOnNewDevices = value;
        }

        /// <summary>
        /// Whether to attempt to re-create the original event timing when replaying events. Disabled by default.
        /// </summary>
        /// <value>If true, events are queued based on their timestamp rather than based on their recorded frames (if any).</value>
        /// <seealso cref="InputEventTrace.ReplayController.PlayAllEventsAccordingToTimestamps"/>
        public bool simulateOriginalTimingOnReplay
        {
            get => m_SimulateOriginalTimingOnReplay;
            set => m_SimulateOriginalTimingOnReplay = value;
        }

        public ChangeEvent changeEvent
        {
            get
            {
                if (m_ChangeEvent == null)
                    m_ChangeEvent = new ChangeEvent();
                return m_ChangeEvent;
            }
        }

        public void StartCapture()
        {
            if (m_EventTrace != null && m_EventTrace.enabled)
                return;

            CreateEventTrace();
            m_EventTrace.Enable();
            m_ChangeEvent?.Invoke(Change.CaptureStarted);
        }

        public void StopCapture()
        {
            if (m_EventTrace != null && m_EventTrace.enabled)
            {
                m_EventTrace.Disable();
                m_ChangeEvent?.Invoke(Change.CaptureStopped);
            }
        }

        public void StartReplay()
        {
            if (m_EventTrace == null)
                return;

            if (replayIsRunning && replay.paused)
            {
                replay.paused = false;
                return;
            }

            StopCapture();

            // Configure replay controller.
            m_ReplayController = m_EventTrace.Replay()
                .OnFinished(StopReplay)
                .OnEvent(_ => m_ChangeEvent?.Invoke(Change.EventPlayed));
            if (m_ReplayOnNewDevices)
                m_ReplayController.WithAllDevicesMappedToNewInstances();

            // Start replay.
            if (m_SimulateOriginalTimingOnReplay)
                m_ReplayController.PlayAllEventsAccordingToTimestamps();
            else
                m_ReplayController.PlayAllFramesOneByOne();

            m_ChangeEvent?.Invoke(Change.ReplayStarted);
        }

        public void StopReplay()
        {
            if (m_ReplayController != null)
            {
                m_ReplayController.Dispose();
                m_ReplayController = null;
                m_ChangeEvent?.Invoke(Change.ReplayStopped);
            }
        }

        public void PauseReplay()
        {
            if (m_ReplayController != null)
                m_ReplayController.paused = true;
        }

        public void ClearCapture()
        {
            m_EventTrace?.Clear();
        }

        public void LoadCaptureFromFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            CreateEventTrace();
            m_EventTrace.ReadFrom(fileName);
        }

        public void SaveCaptureToFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            m_EventTrace?.WriteTo(fileName);
        }

        protected void OnEnable()
        {
            // Hook InputSystem.onEvent before the event trace does.
            HookOnInputEvent();

            if (m_StartRecordingWhenEnabled)
                StartCapture();
        }

        protected void OnDisable()
        {
            StopCapture();
            StopReplay();
            UnhookOnInputEvent();
        }

        protected void OnDestroy()
        {
            m_ReplayController?.Dispose();
            m_ReplayController = null;
            m_EventTrace?.Dispose();
            m_EventTrace = null;
        }

        private bool OnFilterInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            // Filter out non-state events, if enabled.
            if (m_RecordStateEventsOnly && !eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return false;

            // Match device path, if set.
            if (string.IsNullOrEmpty(m_DevicePath) || device == null)
                return true;
            return InputControlPath.MatchesPrefix(m_DevicePath, device);
        }

        private void OnEventRecorded(InputEventPtr eventPtr)
        {
            m_ChangeEvent?.Invoke(Change.EventCaptured);
        }

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;

            if (!string.IsNullOrEmpty(m_PlayButtonPath))
            {
                var playControl = InputControlPath.TryFindControl(device, m_PlayButtonPath) as InputControl<float>;
                if (playControl != null && playControl.ReadValueFromEvent(eventPtr) >= InputSystem.settings.defaultButtonPressPoint)
                {
                    if (replayIsRunning)
                        StopReplay();
                    else
                        StartReplay();

                    eventPtr.handled = true;
                }
            }

            if (!string.IsNullOrEmpty(m_RecordButtonPath))
            {
                var recordControl = InputControlPath.TryFindControl(device, m_RecordButtonPath) as InputControl<float>;
                if (recordControl != null && recordControl.ReadValueFromEvent(eventPtr) >= InputSystem.settings.defaultButtonPressPoint)
                {
                    if (captureIsRunning)
                        StopCapture();
                    else
                        StartCapture();

                    eventPtr.handled = true;
                }
            }
        }

        #if UNITY_EDITOR
        protected void OnValidate()
        {
            if (m_EventTrace != null)
                m_EventTrace.recordFrameMarkers = m_RecordFrames;
        }

        #endif

        [SerializeField] private bool m_StartRecordingWhenEnabled = false;

        [Tooltip("If enabled, additional events will be recorded that demarcate frame boundaries. When replaying, this allows "
            + "spacing out input events across frames corresponding to the original distribution across frames when input was "
            + "recorded. If this is turned off, all input events will be queued in one block when replaying the trace.")]
        [SerializeField] private bool m_RecordFrames = true;

        [Tooltip("If enabled, new devices will be created for captured events when replaying them. If disabled (default), "
            + "events will be queued as is and thus keep their original device ID.")]
        [SerializeField] private bool m_ReplayOnNewDevices;

        [Tooltip("If enabled, the system will try to simulate the original event timing on replay. This differs from replaying frame "
            + "by frame in that replay will try to compensate for differences in frame timings and redistribute events to frames that "
            + "more closely match the original timing. Note that this is not perfect and will not necessarily create a 1:1 match.")]
        [SerializeField] private bool m_SimulateOriginalTimingOnReplay;

        [Tooltip("If enabled, only StateEvents and DeltaStateEvents will be captured.")]
        [SerializeField] private bool m_RecordStateEventsOnly;

        [SerializeField] private int m_CaptureMemoryDefaultSize = 2 * 1024 * 1024;
        [SerializeField] private int m_CaptureMemoryMaxSize = 10 * 1024 * 1024;

        [SerializeField]
        [InputControl(layout = "InputDevice")]
        private string m_DevicePath;

        [SerializeField]
        [InputControl(layout = "Button")]
        private string m_RecordButtonPath;

        [SerializeField]
        [InputControl(layout = "Button")]
        private string m_PlayButtonPath;

        [SerializeField] private ChangeEvent m_ChangeEvent;

        private Action<InputEventPtr, InputDevice> m_OnInputEventDelegate;
        private InputEventTrace m_EventTrace;
        private InputEventTrace.ReplayController m_ReplayController;

        private void CreateEventTrace()
        {
            ////FIXME: remaining configuration should come through, too, if changed after the fact
            if (m_EventTrace == null || m_EventTrace.maxSizeInBytes == 0)
            {
                m_EventTrace?.Dispose();
                m_EventTrace = new InputEventTrace(m_CaptureMemoryDefaultSize, growBuffer: true, maxBufferSizeInBytes: m_CaptureMemoryMaxSize);
            }

            m_EventTrace.recordFrameMarkers = m_RecordFrames;
            m_EventTrace.onFilterEvent += OnFilterInputEvent;
            m_EventTrace.onEvent += OnEventRecorded;
        }

        private void HookOnInputEvent()
        {
            if (string.IsNullOrEmpty(m_PlayButtonPath) && string.IsNullOrEmpty(m_RecordButtonPath))
            {
                UnhookOnInputEvent();
                return;
            }

            if (m_OnInputEventDelegate == null)
                m_OnInputEventDelegate = OnInputEvent;
            InputSystem.onEvent += m_OnInputEventDelegate;
        }

        private void UnhookOnInputEvent()
        {
            if (m_OnInputEventDelegate != null)
                InputSystem.onEvent -= m_OnInputEventDelegate;
        }

        public enum Change
        {
            None,
            EventCaptured,
            EventPlayed,
            CaptureStarted,
            CaptureStopped,
            ReplayStarted,
            ReplayStopped,
        }

        [Serializable]
        public class ChangeEvent : UnityEvent<Change>
        {
        }
    }
}
