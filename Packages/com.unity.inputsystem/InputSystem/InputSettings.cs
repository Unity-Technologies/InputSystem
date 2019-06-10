using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;

////TODO: make sure that alterations made to InputSystem.settings in play mode do not leak out into edit mode or the asset

////TODO: handle case of supportFixedUpdates and supportDynamicUpdates both being set to false; should it be an enum?

////TODO: figure out how this gets into a build

////TODO: allow setting up single- and multi-user configs for the project

////TODO: allow enabling/disabling plugins

////REVIEW: should the project settings include a list of action assets to use? (or to force into a build)

////REVIEW: add extra option to enable late-updates?

////REVIEW: put default sensor sampling frequency here?

////REVIEW: put default gamepad polling frequency here?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Project-wide input settings.
    /// </summary>
    /// <remarks>
    /// Several aspects of the input system can be customized to tailor how the system functions to the
    /// specific needs of a project.
    /// </remarks>
    /// <seealso cref="InputSystem.settings"/>
    /// <seealso cref="InputSystem.onSettingsChange"/>
    public class InputSettings : ScriptableObject
    {
        /// <summary>
        /// Determine how the input system updates, i.e. processing pending input events.
        /// </summary>
        /// <seealso cref="InputSystem.Update()"/>
        /// <seealso cref="timesliceEvents"/>
        public UpdateMode updateMode
        {
            get => m_UpdateMode;
            set
            {
                if (m_UpdateMode == value)
                    return;
                m_UpdateMode = value;
                OnChange();
            }
        }

        public ActionUpdateMode actionUpdateMode
        {
            get
            {
                // Certain update modes force certain action update modes.
                switch (updateMode)
                {
                    case UpdateMode.ProcessEventsInDynamicUpdateOnly:
                        return ActionUpdateMode.UpdateActionsInDynamicUpdate;

                    case UpdateMode.ProcessEventsInFixedUpdateOnly:
                        return ActionUpdateMode.UpdateActionsInFixedUpdate;
                }

                return m_ActionUpdateMode;
            }
            set
            {
                if (m_ActionUpdateMode == value)
                    return;
                m_ActionUpdateMode = value;
                OnChange();
            }
        }

        /// <summary>
        /// If enabled, any given input event will only be processed for any given fixed or dynamic
        /// update if it has been generated before or within the time slice allotted to the update.
        /// </summary>
        /// <remarks>
        /// Normally, the input system will directly consume any input that's available regardless of when
        /// it was produced.
        /// </remarks>
        public bool timesliceEvents
        {
            get => m_TimesliceEvents;
            set
            {
                if (m_TimesliceEvents == value)
                    return;
                m_TimesliceEvents = value;
                OnChange();
            }
        }

        /// <summary>
        /// If true, sensors that deliver rotation values on handheld devices will automatically adjust
        /// rotations when the screen orientation changes.
        /// </summary>
        /// <remarks>
        /// This is enabled by default.
        ///
        /// If enabled, rotation values will be rotated around Z. In <see cref="ScreenOrientation.Portrait"/>, values
        /// remain unchanged. In <see cref="ScreenOrientation.PortraitUpsideDown"/>, they will be rotated by 180 degrees.
        /// In <see cref="ScreenOrientation.LandscapeLeft"/> by 90 degrees, and in <see cref="ScreenOrientation.LandscapeRight"/>
        /// by 270 degrees.
        ///
        /// Sensors affected by this setting are <see cref="Accelerometer"/>, <see cref="Compass"/>, and <see cref="Gyroscope"/>.
        /// </remarks>
        /// <seealso cref="CompensateDirectionProcessor"/>
        public bool compensateForScreenOrientation
        {
            get => m_CompensateForScreenOrientation;
            set
            {
                if (m_CompensateForScreenOrientation == value)
                    return;
                m_CompensateForScreenOrientation = value;
                OnChange();
            }
        }

        public bool filterNoiseOnCurrent
        {
            get => m_FilterNoiseOnCurrent;
            set
            {
                if (m_FilterNoiseOnCurrent == value)
                    return;
                m_FilterNoiseOnCurrent = value;
                OnChange();
            }
        }

        /// <summary>
        /// Default value used when nothing is set explicitly on <see cref="StickDeadzoneProcessor.min"/>
        /// or <see cref="AxisDeadzoneProcessor.min"/>.
        /// </summary>
        /// <seealso cref="StickDeadzoneProcessor"/>
        /// <seealso cref="AxisDeadzoneProcessor"/>
        public float defaultDeadzoneMin
        {
            get => m_DefaultDeadzoneMin;
            set
            {
                if (m_DefaultDeadzoneMin == value)
                    return;
                m_DefaultDeadzoneMin = value;
                OnChange();
            }
        }

        /// <summary>
        /// Default value used when nothing is set explicitly on <see cref="StickDeadzoneProcessor.max"/>
        /// or <see cref="AxisDeadzoneProcessor.max"/>.
        /// </summary>
        /// <seealso cref="StickDeadzoneProcessor"/>
        /// <seealso cref="AxisDeadzoneProcessor"/>
        public float defaultDeadzoneMax
        {
            get => m_DefaultDeadzoneMax;
            set
            {
                if (m_DefaultDeadzoneMax == value)
                    return;
                m_DefaultDeadzoneMax = value;
                OnChange();
            }
        }

        public float defaultButtonPressPoint
        {
            get => m_DefaultButtonPressPoint;
            set
            {
                if (m_DefaultButtonPressPoint == value)
                    return;
                m_DefaultButtonPressPoint = value;
                OnChange();
            }
        }

        public float defaultTapTime
        {
            get => m_DefaultTapTime;
            set
            {
                if (m_DefaultTapTime == value)
                    return;
                m_DefaultTapTime = value;
                OnChange();
            }
        }

        public float defaultSlowTapTime
        {
            get => m_DefaultSlowTapTime;
            set
            {
                if (m_DefaultSlowTapTime == value)
                    return;
                m_DefaultSlowTapTime = value;
                OnChange();
            }
        }

        public float defaultHoldTime
        {
            get => m_DefaultHoldTime;
            set
            {
                if (m_DefaultHoldTime == value)
                    return;
                m_DefaultHoldTime = value;
                OnChange();
            }
        }

        /// <summary>
        /// List of device layouts used by the project.
        /// </summary>
        /// <remarks>
        /// This would usually be one of the high-level abstract device layouts. For example, for
        /// a game that supports touch, gamepad, and keyboard&amp;mouse, the list would be
        /// <c>{ "Touchscreen", "Gamepad", "Mouse", "Keyboard" }</c>. However, nothing prevents the
        /// the user from adding something a lot more specific. A game that can only be played
        /// with a DualShock controller could make this list just be <c>{ "DualShockGamepad" }</c>,
        /// for example.
        ///
        /// In the editor, we use the information to filter what we display to the user by automatically
        /// filtering out irrelevant controls in the control picker and such.
        ///
        /// The information is also used when a new device is discovered. If the device is not listed
        /// as supported by the project, it is ignored.
        ///
        /// The list is empty by default. An empty list indicates that no restrictions are placed on what
        /// devices are supported. In this editor, this means that all possible devices and controls are
        /// shown.
        /// </remarks>
        /// <seealso cref="InputControlLayout"/>
        public ReadOnlyArray<string> supportedDevices
        {
            get => new ReadOnlyArray<string>(m_SupportedDevices);
            set
            {
                // Detect if there was a change.
                if (supportedDevices.Count == value.Count)
                {
                    var hasChanged = false;
                    for (var i = 0; i < supportedDevices.Count; ++i)
                        if (m_SupportedDevices[i] != value[i])
                        {
                            hasChanged = true;
                            break;
                        }

                    if (!hasChanged)
                        return;
                }

                m_SupportedDevices = value.ToArray();
                OnChange();
            }
        }

        [Tooltip("Determine which type of devices are used by the application. By default, this is empty meaning that all devices recognized "
            + "by Unity will be used. Restricting the set of supported devices will make only those devices appear in the input system.")]
        [SerializeField] private string[] m_SupportedDevices;
        [Tooltip("Determine when Unity processes events. By default, accumulated input events are flushed out before each fixed update and "
            + "before each dynamic update. This setting can be used to restrict event processing to only where the application needs it.")]
        [SerializeField] private UpdateMode m_UpdateMode;
        [Tooltip("Determine when input actions are triggered. This is only relevant if both fixed and dynamic updates are enabled. By default, "
            + "actions are triggered right before fixed updates.")]
        [SerializeField] private ActionUpdateMode m_ActionUpdateMode;

        [Tooltip("Whether events should be distributed across updates according to their timestamps. This is most relevant when fixed "
            + "updates are enabled. If enabled, the system will compute a real-time time span corresponding to each update and will process only "
            + "those events that have timestamps within or before that time span.")]
        [SerializeField] private bool m_TimesliceEvents = true;
        [SerializeField] private bool m_CompensateForScreenOrientation = true;
        [SerializeField] private bool m_FilterNoiseOnCurrent = false;

        [SerializeField] private float m_DefaultDeadzoneMin = 0.125f;
        [SerializeField] private float m_DefaultDeadzoneMax = 0.925f;
        [SerializeField] private float m_DefaultButtonPressPoint = 0.5f;
        [SerializeField] private float m_DefaultTapTime = 0.2f;
        [SerializeField] private float m_DefaultSlowTapTime = 0.5f;
        //[SerializeField] private float m_DefaultMultiTapMaximumDelay = 0.75f;
        [SerializeField] private float m_DefaultHoldTime = 0.4f;

        #if UNITY_EDITOR
        [SerializeField] private bool m_LockInputToGameView;
        #endif

        internal void OnChange()
        {
            if (InputSystem.settings == this)
                InputSystem.s_Manager.ApplySettings();
        }

        /// <summary>
        /// How the input system should update.
        /// </summary>
        /// <remarks>
        /// By default, the input system will run event processing as part of the player loop. In the default configuration,
        /// the processing will happens once before every fixed update (<see cref="FixedUpdate"/>) and once
        /// before every dynamic update (<see cref="Update"/>), i.e. <see cref="ProcessEventsInBothFixedAndDynamicUpdate"/>
        /// is the default behavior.
        ///
        /// Note that as dynamic and fixed update represent different timelines, input state (<see cref="InputStateBlock"/>)
        /// is stored separately for them. This means that if both dynamic and fixed updates are enabled, twice the memory
        /// is consumed as compared to enabling only one of the updates.
        ///
        /// There are two types of updates not governed by UpdateMode. One is <see cref="InputUpdateType.Editor"/> which
        /// will always be enabled in the editor and govern input updates for <see cref="UnityEditor.EditorWindow"/>s in
        /// sync to <see cref="UnityEditor.EditorApplication.update"/>.
        ///
        /// The other update type is <see cref="InputUpdateType.BeforeRender"/>. This type of update is enabled and disabled
        /// automatically in response to whether devices are present requiring this type of update (<see
        /// cref="InputDevice.updateBeforeRender"/>). This update does not consume extra state.
        /// </remarks>
        /// <seealso cref="InputSystem.Update()"/>
        /// <seealso cref="InputUpdateType"/>
        /// <seealso cref="FixedUpdate"/>
        /// <seealso cref="Update"/>
        public enum UpdateMode
        {
            /// <summary>
            /// Automatically run updates both right before <see cref="FixedUpdate"/> and right before
            /// <see cref="Update"/>.
            /// </summary>
            /// <seealso cref="FixedUpdate"/>
            /// <seealso cref="Update"/>
            ProcessEventsInBothFixedAndDynamicUpdate,

            /// <summary>
            /// Automatically run updates only right before <see cref="Update"/>.
            /// </summary>
            /// <remarks>
            /// In this mode, no processing happens specifically for fixed updates. Querying input state in
            /// <see cref="FixedUpdate"/> will result in errors being logged in the editor and in
            /// development builds. In release player builds, the value of the dynamic update state is returned.
            /// </remarks>
            ProcessEventsInDynamicUpdateOnly,

            /// <summary>
            /// Automatically run updates only right before <see cref="FixedUpdate"/>.
            /// </summary>
            /// <remarks>
            /// In this mode, no processing happens specifically for dynamic updates. Querying input state in
            /// <see cref="Update"/> will result in errors being logged in the editor and in
            /// development builds. In release player builds, the value of the fixed update state is returned.
            /// </remarks>
            ProcessEventsInFixedUpdateOnly,

            /// <summary>
            /// Do not run updates automatically. In this mode, <see cref="InputSystem.Update()"/> must be called
            /// manually to update input.
            /// </summary>
            /// <remarks>
            /// This mode is most useful for placing input updates in the frame explicitly at an exact location.
            ///
            /// Note that failing to call <see cref="InputSystem.Update()"/> may result in a lot of events
            /// accumulating or some input getting lost.
            /// </remarks>
            ProcessEventsManually,
        }

        public enum ActionUpdateMode
        {
            UpdateActionsInFixedUpdate,
            UpdateActionsInDynamicUpdate,
        }
    }
}
