using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Controls;
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

////REVIEW: Have an InputActionAsset field in here that allows having a single default set of actions that are enabled with no further setup?

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
    public partial class InputSettings : ScriptableObject
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
                // ReSharper disable once CompareOfFloatsByEqualityOperator
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
                // ReSharper disable once CompareOfFloatsByEqualityOperator
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
        /// Any value will implicitly be clamped to <c>0.0001f</c> as allowing a value of 0 would
        /// cause all buttons in their default state to already be pressed.
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
        /// <seealso cref="buttonReleaseThreshold"/>
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
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (m_DefaultButtonPressPoint == value)
                    return;
                m_DefaultButtonPressPoint = Mathf.Clamp(value, ButtonControl.kMinButtonPressPoint, float.MaxValue);
                OnChange();
            }
        }

        /// <summary>
        /// The percentage of <see cref="defaultButtonPressPoint"/> at which a button that was pressed
        /// is considered released again.
        /// </summary>
        /// <remarks>
        /// This setting helps avoid flickering around the button press point by introducing something akin to a
        /// "dead zone" below <see cref="defaultButtonPressPoint"/>. Once a button has been pressed to a magnitude
        /// of at least <see cref="defaultButtonPressPoint"/>, it is considered pressed and keeps being considered pressed
        /// until its magnitude falls back to a value of or below <see cref="buttonReleaseThreshold"/> percent of
        /// <see cref="defaultButtonPressPoint"/>.
        ///
        /// This is a percentage rather than a fixed value so it allows computing release
        /// points even when the press point has been customized. If, for example, a <see cref="Interactions.PressInteraction"/>
        /// sets a custom <see cref="Interactions.PressInteraction.pressPoint"/>, the respective release point
        /// can still be computed from the percentage set here.
        /// </remarks>
        public float buttonReleaseThreshold
        {
            get => m_ButtonReleaseThreshold;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (m_ButtonReleaseThreshold == value)
                    return;
                m_ButtonReleaseThreshold = value;
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
        /// <seealso cref="Interactions.TapInteraction"/>
        public float defaultTapTime
        {
            get => m_DefaultTapTime;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
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
                // ReSharper disable once CompareOfFloatsByEqualityOperator
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
                // ReSharper disable once CompareOfFloatsByEqualityOperator
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
                // ReSharper disable once CompareOfFloatsByEqualityOperator
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
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (m_MultiTapDelayTime == value)
                    return;
                m_MultiTapDelayTime = value;
                OnChange();
            }
        }

        /// <summary>
        /// When <c>Application.runInBackground</c> is true, this property determines what happens when application focus changes
        /// (see <a href="https://docs.unity3d.com/ScriptReference/Application-isFocused.html">Application.isFocused</a>) changes and how we handle
        /// input while running the background.
        /// </summary>
        /// <value>What to do with input while not having focus. Set to <see cref="BackgroundBehavior.ResetAndDisableNonBackgroundDevices"/> by default.</value>
        /// <remarks>
        /// If <c>Application.runInBackground</c> is false, the value of this property is ignored. In that case, nothing happens when
        /// focus is lost. However, when focus is regained, <see cref="InputSystem.TrySyncDevice"/> is called on all devices.
        ///
        /// Note that in the editor as well as in development standalone players, <c>Application.runInBackground</c> will effectively always be
        /// turned on. The editor keeps the player loop running regardless of Game View focus for as long as the editor is active and in play mode
        /// and development players will implicitly turn on the setting during the build process.
        /// </remarks>
        /// <seealso cref="InputSystem.ResetDevice"/>
        /// <seealso cref="InputSystem.EnableDevice"/>
        /// <seealso cref="InputDevice.canRunInBackground"/>
        /// <seealso cref="editorInputBehaviorInPlayMode"/>
        public BackgroundBehavior backgroundBehavior
        {
            get => m_BackgroundBehavior;
            set
            {
                if (m_BackgroundBehavior == value)
                    return;
                m_BackgroundBehavior = value;
                OnChange();
            }
        }

        /// <summary>
        /// Determines how player focus is handled in the editor with respect to input.
        /// </summary>
        /// <remarks>
        /// This setting only has an effect while in play mode (see <a href="https://docs.unity3d.com/ScriptReference/Application-isPlaying.html">Application.isPlaying</a>).
        /// While not in play mode, all input is invariably routed to the editor.
        ///
        /// The editor generally treats Game View focus as equivalent to application focus (see <a href="https://docs.unity3d.com/ScriptReference/Application-isFocused.html">Application.isFocused</a>).
        /// In other words, as long as any Game View has focus, the player is considered to have input focus. As soon as focus is transferred to a non-Game View
        /// <c>EditorWindow</c> or the editor as a whole loses focus, the player is considered to have lost input focus.
        ///
        /// However, unlike in built players, the editor will keep running the player loop while in play mode regardless of whether a Game View is focused
        /// or not. This essentially equates to <a href="https://docs.unity3d.com/ScriptReference/Application-runInBackground.html">Application.runInBackground</a> always
        /// being true in the editor.
        ///
        /// To accommodate this behavior, this setting determines where input is routed while the player loop is running with no Game View being focused. As such,
        /// it also dictates which input reaches the editor (if any) while the game is playing.
        /// </remarks>
        /// <seealso cref="backgroundBehavior"/>
        public EditorInputBehaviorInPlayMode editorInputBehaviorInPlayMode
        {
            get => m_EditorInputBehaviorInPlayMode;
            set
            {
                if (m_EditorInputBehaviorInPlayMode == value)
                    return;
                m_EditorInputBehaviorInPlayMode = value;
                OnChange();
            }
        }

        /// <summary>
        /// Upper limit on the amount of bytes worth of <see cref="InputEvent"/>s processed in a single
        /// <see cref="InputSystem.Update"/>.
        /// </summary>
        /// <remarks>
        /// This setting establishes a bound on the amount of input event data processed in a single
        /// update and thus limits throughput allowed for input. This prevents long stalls from
        /// leading to long delays in input processing.
        ///
        /// When the limit is exceeded, all events remaining in the buffer are thrown away (the
        /// <see cref="InputEventBuffer"/> is reset) and an error is logged. After that, the current
        /// update will abort and early out.
        ///
        /// Setting this property to 0 or a negative value will disable the limit.
        ///
        /// The default value is 5MB.
        /// </remarks>
        /// <seealso cref="InputSystem.Update"/>
        /// <see cref="InputEvent.sizeInBytes"/>
        public int maxEventBytesPerUpdate
        {
            get => m_MaxEventBytesPerUpdate;
            set
            {
                if (m_MaxEventBytesPerUpdate == value)
                    return;
                m_MaxEventBytesPerUpdate = value;
                OnChange();
            }
        }

        /// <summary>
        /// Upper limit on the number of <see cref="InputEvent"/>s that can be queued within one
        /// <see cref="InputSystem.Update"/>.
        /// <remarks>
        /// This settings establishes an upper limit on the number of events that can be queued
        /// using <see cref="InputSystem.QueueEvent"/> during a single update. This prevents infinite
        /// loops where an action callback queues an event that causes the action callback to
        /// be called again which queues an event...
        ///
        /// Note that this limit only applies while the input system is updating. There is no limit
        /// on the number of events that can be queued outside of this time, but those will be queued
        /// into the next frame where the <see cref="maxEventBytesPerUpdate"/> setting will apply.
        ///
        /// The default value is 1000.
        /// </remarks>
        /// </summary>
        public int maxQueuedEventsPerUpdate
        {
            get => m_MaxQueuedEventsPerUpdate;
            set
            {
                if (m_MaxQueuedEventsPerUpdate == value)
                    return;

                m_MaxQueuedEventsPerUpdate = value;
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

        /// <summary>
        /// Disables merging of redundant input events (at the moment, only mouse events).
        /// Disable it if you want to get all events.
        /// </summary>
        /// <remarks>
        /// When using a high frequency mouse, the number of mouse move events in each frame can be
        /// very large, which can have a negative effect on performance. To help with this,
        /// merging events can be used which coalesces consecutive mouse move events into a single
        /// input action update.
        ///
        /// For example, if there are one hundred mouse events, but they are all position updates
        /// with no clicks, and there is an input action callback handler for the mouse position, that
        /// callback handler will only be called one time in the current frame. Delta and scroll
        /// values for the mouse will still be accumulated across all mouse events.
        /// </remarks>
        public bool disableRedundantEventsMerging
        {
            get => m_DisableRedundantEventsMerging;
            set
            {
                if (m_DisableRedundantEventsMerging == value)
                    return;

                m_DisableRedundantEventsMerging = value;
                OnChange();
            }
        }

        /// <summary>
        /// Enable or disable an internal feature by its name.
        /// </summary>
        /// <param name="featureName">Name of the feature.</param>
        /// <param name="enabled">Whether to enable or disable the feature.</param>
        /// <exception cref="ArgumentNullException"><paramref name="featureName"/> is <c>null</c> or empty.</exception>
        /// <remarks>
        /// This method is intended for experimental features. These must be enabled/disabled from code.
        /// Setting or unsetting a feature flag will not be persisted in an <c>.inputsettings</c> file.
        /// </remarks>
        public void SetInternalFeatureFlag(string featureName, bool enabled)
        {
            if (string.IsNullOrEmpty(featureName))
                throw new ArgumentNullException(nameof(featureName));

            if (m_FeatureFlags == null)
                m_FeatureFlags = new HashSet<string>();

            if (enabled)
                m_FeatureFlags.Add(featureName.ToUpperInvariant());
            else
                m_FeatureFlags.Remove(featureName.ToUpperInvariant());

            OnChange();
        }

        [Tooltip("Determine which type of devices are used by the application. By default, this is empty meaning that all devices recognized "
            + "by Unity will be used. Restricting the set of supported devices will make only those devices appear in the input system.")]
        [SerializeField] private string[] m_SupportedDevices;
        [Tooltip("Determine when Unity processes events. By default, accumulated input events are flushed out before each fixed update and "
            + "before each dynamic update. This setting can be used to restrict event processing to only where the application needs it.")]
        [SerializeField] private UpdateMode m_UpdateMode = UpdateMode.ProcessEventsInDynamicUpdate;
        [SerializeField] private int m_MaxEventBytesPerUpdate = 5 * 1024 * 1024;
        [SerializeField] private int m_MaxQueuedEventsPerUpdate = 1000;

        [SerializeField] private bool m_CompensateForScreenOrientation = true;
        [SerializeField] private bool m_FilterNoiseOnCurrent = false;
        [SerializeField] private BackgroundBehavior m_BackgroundBehavior = BackgroundBehavior.ResetAndDisableNonBackgroundDevices;
        [SerializeField] private EditorInputBehaviorInPlayMode m_EditorInputBehaviorInPlayMode;
        [SerializeField] private float m_DefaultDeadzoneMin = 0.125f;
        [SerializeField] private float m_DefaultDeadzoneMax = 0.925f;
        // A setting of 0.5 seems to roughly be what games generally use on the gamepad triggers.
        // Having a higher value here also obsoletes the need for custom press points on stick buttons
        // (the up/down/left/right ones).
        [Min(ButtonControl.kMinButtonPressPoint)]
        [SerializeField] private float m_DefaultButtonPressPoint = 0.5f;
        [SerializeField] private float m_ButtonReleaseThreshold = 0.75f;
        [SerializeField] private float m_DefaultTapTime = 0.2f;
        [SerializeField] private float m_DefaultSlowTapTime = 0.5f;
        [SerializeField] private float m_DefaultHoldTime = 0.4f;
        [SerializeField] private float m_TapRadius = 5;
        [SerializeField] private float m_MultiTapDelayTime = 0.75f;
        [SerializeField] private bool m_DisableRedundantEventsMerging = false;

        [NonSerialized] internal HashSet<string> m_FeatureFlags;

        internal bool IsFeatureEnabled(string featureName)
        {
            return m_FeatureFlags != null && m_FeatureFlags.Contains(featureName.ToUpperInvariant());
        }

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
        /// the processing will happens once before every every dynamic update (<a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html">Update</a>),
        /// i.e. <see cref="ProcessEventsInDynamicUpdate"/> is the default behavior.
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
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html"/>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html"/>
        public enum UpdateMode
        {
            // Removed: ProcessEventsInBothFixedAndDynamicUpdate=0

            /// <summary>
            /// Automatically run input updates right before every <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html">Update</a>.
            ///
            /// In this mode, no processing happens specifically for fixed updates. Querying input state in
            /// <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html">FixedUpdate</a> will result in errors being logged in the editor and in
            /// development builds. In release player builds, the value of the dynamic update state is returned.
            /// </summary>
            ProcessEventsInDynamicUpdate = 1,

            /// <summary>
            /// Automatically input run updates right before every <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html">FixedUpdate</a>.
            ///
            /// In this mode, no processing happens specifically for dynamic updates. Querying input state in
            /// <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html">Update</a> will result in errors being logged in the editor and in
            /// development builds. In release player builds, the value of the fixed update state is returned.
            /// </summary>
            ProcessEventsInFixedUpdate,

            /// <summary>
            /// Do not run updates automatically. In this mode, <see cref="InputSystem.Update"/> must be called
            /// manually to update input.
            ///
            /// This mode is most useful for placing input updates in the frame explicitly at an exact location.
            ///
            /// Note that failing to call <see cref="InputSystem.Update"/> may result in a lot of events
            /// accumulating or some input getting lost.
            /// </summary>
            ProcessEventsManually,
        }

        /// <summary>
        /// Determines how the applications behaves when running in the background. See <see cref="backgroundBehavior"/>.
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Application-isFocused.html"/>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Application-runInBackground.html"/>
        /// <seealso cref="backgroundBehavior"/>
        /// <seealso cref="InputSettings.editorInputBehaviorInPlayMode"/>
        public enum BackgroundBehavior
        {
            /// <summary>
            /// When the application loses focus, issue a <see cref="InputSystem.ResetDevice"/> call on every <see cref="InputDevice"/> that is
            /// not marked as <see cref="InputDevice.canRunInBackground"/> and then disable the device (see <see cref="InputSystem.DisableDevice"/>
            /// and <see cref="InputDevice.enabled"/>). Devices that <see cref="InputDevice.canRunInBackground"/> will not be touched and will
            /// keep running as is.
            ///
            /// In effect, this setting will "soft-reset" all devices that cannot receive input while the application does
            /// not have focus. That is, it will reset all controls that are not marked as <see cref="InputControlLayout.ControlItem.dontReset"/>
            /// to their default state.
            ///
            /// When the application comes back into focus, all devices that have been reset and disabled will be re-enabled and a synchronization
            /// request (see <see cref="RequestSyncCommand"/>) will be sent to each device.
            ///
            /// Devices that are added while the application is running in the background are treated like devices that were already present
            /// when losing focus. That is, if they cannot run in the background, they will be disabled until focus comes back.
            ///
            /// Note that the resets will cancel <see cref="InputAction"/>s that are in progress from controls on devices that are being reset.
            /// </summary>
            ResetAndDisableNonBackgroundDevices = 0,

            /// <summary>
            /// Like <see cref="ResetAndDisableNonBackgroundDevices"/> but instead treat all devices as having <see cref="InputDevice.canRunInBackground"/>
            /// return false. This effectively means that all input is silenced while the application is running in the background.
            /// </summary>
            ResetAndDisableAllDevices = 1,

            /// <summary>
            /// Ignore all changes in focus and leave devices untouched. This also disables focus checks in <see cref="UI.InputSystemUIInputModule"/>.
            /// </summary>
            IgnoreFocus = 2,
        }

        /// <summary>
        /// Determines how player focus is handled with respect to input when we are in play mode in the editor.
        /// See <see cref="InputSettings.editorInputBehaviorInPlayMode"/>. The setting does not have an effect
        /// when the editor is not in play mode.
        /// </summary>
        public enum EditorInputBehaviorInPlayMode
        {
            /// <summary>
            /// When the game view does not have focus, input from <see cref="Pointer"/> devices (such as <see cref="Mouse"/> and <see cref="Touchscreen"/>)
            /// is routed to the editor and not visible in player code. Input from devices such as <see cref="Gamepad"/>s will continue to
            /// go to the game regardless of which <c>EditorWindow</c> is focused.
            ///
            /// This is the default. It makes sure that the devices that are used with the editor UI respect <c>EditorWindow</c> focus and thus
            /// do not lead to accidental inputs affecting the game. While at the same time letting all other input get through to the game.
            /// It does, however, mean that no other <c>EditorWindow</c> can tap input from these devices (such as <see cref="Gamepad"/>s and <see cref="HID"/>s).
            /// </summary>
            PointersAndKeyboardsRespectGameViewFocus,

            /// <summary>
            /// When the game view does not have focus, all input is routed to the editor (and thus <c>EditorWindow</c>s). No input
            /// is received in the game regardless of the type of device generating it.
            /// </summary>
            AllDevicesRespectGameViewFocus,

            /// <summary>
            /// All input is going to the game at all times. This most closely aligns input behavior in the editor with that in players. <see cref="backgroundBehavior"/>
            /// will be respected as in the player and devices may thus be disabled in the runtime based on Game View focus.
            /// </summary>
            AllDeviceInputAlwaysGoesToGameView,
        }
    }
}
