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
    /// specific needs of a project. These settings are collected in this class. There is one global
    /// settings object active at any one time. It can be accessed and set through <see cref="InputSystem.settings"/>.
    ///
    /// Changing a setting on the object takes effect immediately. It also triggers the
    /// <see cref="InputSystem.onSettingsChange"/> callback.
    /// </remarks>
    /// <seealso cref="InputSystem.settings"/>
    /// <seealso cref="InputSystem.onSettingsChange"/>
    public class InputSettings : ScriptableObject
    {
        /// <summary>
        /// Determine how the input system updates, i.e. processes pending input events.
        /// </summary>
        /// <value>When to run input updates.</value>
        /// <remarks>
        /// By default, input updates will automatically be triggered as part of the player loop.
        /// If <c>updateMode</c> is set to <see cref="UpdateMode.ProcessEventsInDynamicUpdate"/>
        /// (the default), then right at the beginning of a dynamic update (i.e. before all
        /// <c>MonoBehaviour.Update</c> methods are called), input is processed. And if <c>updateMode</c>
        /// is set to <see cref="UpdateMode.ProcessEventsInFixedUpdate"/>, then right at the beginning
        /// of each fixed update (i.e. before all <c>MonoBehaviour.FixedUpdate</c> methods are
        /// called), input is processed.
        ///
        /// Additionally, if there are devices that need updates right before rendering (see <see
        /// cref="InputDevice.updateBeforeRender"/>), an extra update will be run right before
        /// rendering. This special update will only consume input on devices that have
        /// <see cref="InputDevice.updateBeforeRender"/> set to <c>true</c>.
        ///
        /// You can run updates manually using <see cref="InputSystem.Update"/>. Doing so
        /// outside of tests is only recommended, however, if <c>updateMode</c> is set to
        /// <see cref="UpdateMode.ProcessEventsManually"/> (in which case it is actually required
        /// for input to be processed at all).
        ///
        /// Note that in the editor, input updates will also run before each editor update
        /// (i.e. as part of <c>EditorApplication.update</c>). Player and editor input state
        /// are kept separate, though, so any input consumed in editor updates will not be visible
        /// in player updates and vice versa.
        /// </remarks>
        /// <seealso cref="InputSystem.Update"/>
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

        /// <summary>
        /// Whether to not make a device <c>.current</c> (see <see cref="InputDevice.MakeCurrent"/>)
        /// when there is only noise in the input.
        /// </summary>
        /// <value>Whether to check input on devices for noise.</value>
        /// <remarks>
        /// This is <em>disabled by default</em>.
        ///
        /// When toggled on, this property adds extra processing every time input is
        /// received on a device that is considered noisy. These devices are those that
        /// have at least one control that is marked as <see cref="InputControl.noisy"/>.
        /// A good example is the PS4 controller which has a gyroscope sensor built into
        /// the device. Whereas sticks and buttons on the device require user interaction
        /// to produce non-default values, the gyro will produce varying values even if
        /// the device just sits there without user interaction.
        ///
        /// Without noise filtering, a PS4 controller will thus continually make itself
        /// current as it will send a continuous stream of input even when not actively
        /// used by the player. By toggling this property on, each input event will be
        /// run through a noise mask. Only if state has changed outside of memory areas
        /// marked as noise will the input be considered valid user interaction and the
        /// device will be made current. Note that in this process, the system does
        /// <em>not</em> determine whether non-noisy controls on the device have actually
        /// changed value. All the system establishes is whether such controls have changed
        /// <em>state</em>. However, processing such as for deadzones may cause values
        /// to not effectively change even though the non-noisy state of the device has
        /// changed.
        /// </remarks>
        /// <seealso cref="InputDevice.MakeCurrent"/>
        /// <seealso cref="InputControl.noisy"/>
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
        /// <value>Default lower limit for deadzones.</value>
        /// <remarks>
        /// "Deadzones" refer to limits established for the range of values accepted as input
        /// on a control. If the value for the control falls outside the range, i.e. below the
        /// given minimum or above the given maximum, the value is clamped to the respective
        /// limit.
        ///
        /// This property configures the default lower bound of the value range.
        ///
        /// Note that deadzones will by default re-normalize values after clamping. This means that
        /// inputs at the lower and upper end are dropped and that the range in-between is re-normalized
        /// to [0..1].
        ///
        /// Note that deadzones preserve the sign of inputs. This means that both the upper and
        /// the lower deadzone bound extend to both the positive and the negative range. For example,
        /// a deadzone min of 0.1 will clamp values between -0.1 and +0.1.
        ///
        /// The most common example of where deadzones are used are the sticks on gamepads, i.e.
        /// <see cref="Gamepad.leftStick"/> and <see cref="Gamepad.rightStick"/>. Sticks will
        /// usually be wobbly to some extent (just how wobbly varies greatly between different
        /// types of controllers -- which means that often deadzones need to be configured on a
        /// per-device type basis). Using deadzones, stick motion at the extreme ends of the spectrum
        /// can be filtered out and noise in these areas can effectively be eliminated this way.
        ///
        /// The default value for this property is 0.125.
        /// </remarks>
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
        /// <value>Default upper limit for deadzones.</value>
        /// <remarks>
        /// "Deadzones" refer to limits established for the range of values accepted as input
        /// on a control. If the value for the control falls outside the range, i.e. below the
        /// given minimum or above the given maximum, the value is clamped to the respective
        /// limit.
        ///
        /// This property configures the default upper bound of the value range.
        ///
        /// Note that deadzones will by default re-normalize values after clamping. This means that
        /// inputs at the lower and upper end are dropped and that the range in-between is re-normalized
        /// to [0..1].
        ///
        /// Note that deadzones preserve the sign of inputs. This means that both the upper and
        /// the lower deadzone bound extend to both the positive and the negative range. For example,
        /// a deadzone max of 0.95 will clamp values of &gt;0.95 and &lt;-0.95.
        ///
        /// The most common example of where deadzones are used are the sticks on gamepads, i.e.
        /// <see cref="Gamepad.leftStick"/> and <see cref="Gamepad.rightStick"/>. Sticks will
        /// usually be wobbly to some extent (just how wobbly varies greatly between different
        /// types of controllers -- which means that often deadzones need to be configured on a
        /// per-device type basis). Using deadzones, stick motion at the extreme ends of the spectrum
        /// can be filtered out and noise in these areas can effectively be eliminated this way.
        ///
        /// The default value for this property is 0.925.
        /// </remarks>
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

        /// <summary>
        /// The default value threshold for when a button is considered pressed. Used if
        /// no explicit thresholds are set on parameters such as <see cref="Controls.ButtonControl.pressPoint"/>
        /// or <see cref="Interactions.PressInteraction.pressPoint"/>.
        /// </summary>
        /// <value>Default button press threshold.</value>
        /// <remarks>
        /// In the input system, each button constitutes a full floating-point value. Pure
        /// toggle buttons, such as <see cref="Gamepad.buttonSouth"/> for example, will simply
        /// alternate between 0 (not pressed) and 1 (pressed). However, buttons may also have
        /// ranges, such as <see cref="Gamepad.leftTrigger"/> for example. When used in a context
        /// where a clear distinction between pressed and not pressed is required, we need a value
        /// beyond which we consider the button pressed.
        ///
        /// By setting this property, the default value for this can be configured. If a button
        /// has a value equal to or greater than the button press point, it is considered pressed.
        ///
        /// The default value is 0.5.
        ///
        /// Lowering the button press point will make triggers feel more like hair-triggers (akin
        /// to using the hair-trigger feature on Xbox Elite controllers). However, it may make using
        /// the directional buttons (i.e. <see cref="Controls.StickControl.up"/> etc) be fickle as
        /// solely moving in only one direction with sticks isn't easy. To counteract that, the button
        /// press points on the stick buttons can be raised.
        ///
        /// Another solution is to simply lower the press points on the triggers specifically.
        ///
        /// <example>
        /// <code>
        /// InputSystem.RegisterLayoutOverride(@"
        ///     {
        ///         ""name"" : ""HairTriggers"",
        ///         ""extend"" : ""Gamepad"",
        ///         ""controls"" [
        ///             { ""name"" : ""leftTrigger"", ""parameters"" : ""pressPoint=0.1"" },
        ///             { ""name"" : ""rightTrigger"", ""parameters"" : ""pressPoint=0.1"" }
        ///         ]
        ///     }
        /// ");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="Controls.ButtonControl.pressPoint"/>
        /// <seealso cref="Controls.ButtonControl.isPressed"/>
        /// <seealso cref="Interactions.PressInteraction.pressPoint"/>
        /// <seealso cref="Interactions.TapInteraction.pressPoint"/>
        /// <seealso cref="Interactions.SlowTapInteraction.pressPoint"/>
        /// <seealso cref="InputBindingCompositeContext.ReadValueAsButton"/>
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

        /// <summary>
        /// Default time (in seconds) within which a press and release has to occur for it
        /// to be registered as a "tap".
        /// </summary>
        /// <value>Default upper limit on press durations for them to register as taps.</value>
        /// <remarks>
        /// A tap is considered as a quick press-and-release on a button-like input control.
        /// This property determines just how quick the press-and-release has to be, i.e. what
        /// the maximum time is that can elapse between the button being pressed and released
        /// again. If the delay between press and release is greater than this time, the
        /// input does not qualify as a tap.
        ///
        /// The default tap time is 0.2 seconds.
        /// </remarks>
        /// <seealso cref="TapInteraction"/>
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

        public float tapRadius
        {
            get => m_TapRadius;
            set
            {
                if (m_TapRadius == value)
                    return;
                m_TapRadius = value;
                OnChange();
            }
        }

        public float multiTapDelayTime
        {
            get => m_MultiTapDelayTime;
            set
            {
                if (m_MultiTapDelayTime == value)
                    return;
                m_MultiTapDelayTime = value;
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
        [SerializeField] private UpdateMode m_UpdateMode = UpdateMode.ProcessEventsInDynamicUpdate;

        [SerializeField] private bool m_CompensateForScreenOrientation = true;
        [SerializeField] private bool m_FilterNoiseOnCurrent = false;
        [SerializeField] private float m_DefaultDeadzoneMin = 0.125f;
        [SerializeField] private float m_DefaultDeadzoneMax = 0.925f;
        // A setting of 0.5 seems to roughly be what games generally use on the gamepad triggers.
        // Having a higher value here also obsoletes the need for custom press points on stick buttons
        // (the up/down/left/right ones).
        [SerializeField] private float m_DefaultButtonPressPoint = 0.5f;
        [SerializeField] private float m_DefaultTapTime = 0.2f;
        [SerializeField] private float m_DefaultSlowTapTime = 0.5f;
        [SerializeField] private float m_DefaultHoldTime = 0.4f;
        [SerializeField] private float m_TapRadius = 5;
        [SerializeField] private float m_MultiTapDelayTime = 0.75f;

        internal void OnChange()
        {
            if (InputSystem.settings == this)
                InputSystem.s_Manager.ApplySettings();
        }

        internal const int s_OldUnsupportedFixedAndDynamicUpdateSetting = 0;

        /// <summary>
        /// How the input system should update.
        /// </summary>
        /// <remarks>
        /// By default, the input system will run event processing as part of the player loop. In the default configuration,
        /// the processing will happens once before every every dynamic update (<see cref="Update"/>), i.e. <see cref="ProcessEventsInDynamicUpdate"/>
        /// is the default behavior.
        ///
        /// There are two types of updates not governed by UpdateMode. One is <see cref="InputUpdateType.Editor"/> which
        /// will always be enabled in the editor and govern input updates for <see cref="UnityEditor.EditorWindow"/>s in
        /// sync to <see cref="UnityEditor.EditorApplication.update"/>.
        ///
        /// The other update type is <see cref="InputUpdateType.BeforeRender"/>. This type of update is enabled and disabled
        /// automatically in response to whether devices are present requiring this type of update (<see
        /// cref="InputDevice.updateBeforeRender"/>). This update does not consume extra state.
        /// </remarks>
        /// <seealso cref="InputSystem.Update"/>
        /// <seealso cref="InputUpdateType"/>
        /// <seealso cref="MonoBehaviour.FixedUpdate"/>
        /// <seealso cref="MonoBehaviour.Update"/>
        public enum UpdateMode
        {
            // Removed: ProcessEventsInBothFixedAndDynamicUpdate=0

            /// <summary>
            /// Automatically run input updates right before every <see cref="MonoBehaviour.Update"/>.
            /// </summary>
            /// <remarks>
            /// In this mode, no processing happens specifically for fixed updates. Querying input state in
            /// <see cref="MonoBehaviour.FixedUpdate"/> will result in errors being logged in the editor and in
            /// development builds. In release player builds, the value of the dynamic update state is returned.
            /// </remarks>
            ProcessEventsInDynamicUpdate = 1,

            /// <summary>
            /// Automatically input run updates right before every <see cref="MonoBehaviour.FixedUpdate"/>.
            /// </summary>
            /// <remarks>
            /// In this mode, no processing happens specifically for dynamic updates. Querying input state in
            /// <see cref="MonoBehaviour.Update"/> will result in errors being logged in the editor and in
            /// development builds. In release player builds, the value of the fixed update state is returned.
            /// </remarks>
            ProcessEventsInFixedUpdate,

            /// <summary>
            /// Do not run updates automatically. In this mode, <see cref="InputSystem.Update"/> must be called
            /// manually to update input.
            /// </summary>
            /// <remarks>
            /// This mode is most useful for placing input updates in the frame explicitly at an exact location.
            ///
            /// Note that failing to call <see cref="InputSystem.Update"/> may result in a lot of events
            /// accumulating or some input getting lost.
            /// </remarks>
            ProcessEventsManually,
        }
    }
}
