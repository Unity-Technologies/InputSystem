using UnityEngine.InputSystem.Controls;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif

////TODO: add ability to respond to any of the taps in the sequence (e.g. one response for single tap, another for double tap)

////TODO: add ability to perform on final press rather than on release

////TODO: change this so that the interaction stays performed when the tap count is reached until the button is released

namespace UnityEngine.InputSystem.Interactions
{
    ////REVIEW: Why is this deriving from IInputInteraction<float>? It goes by actuation just like Hold etc.
    /// <summary>
    /// Interaction that requires multiple taps (press and release within <see cref="tapTime"/>) spaced no more
    /// than <see cref="tapDelay"/> seconds apart. This equates to a chain of <see cref="TapInteraction"/> with
    /// a maximum delay between each tap.
    /// </summary>
    /// <remarks>
    /// The interaction goes into <see cref="InputActionPhase.Started"/> on the first press and then will not
    /// trigger again until either the full tap sequence is performed (in which case the interaction triggers
    /// <see cref="InputActionPhase.Performed"/>) or the multi-tap is aborted by a timeout being hit (in which
    /// case the interaction will trigger <see cref="InputActionPhase.Canceled"/>).
    /// </remarks>
    public class MultiTapInteraction : IInputInteraction<float>
    {
        /// <summary>
        /// The time in seconds within which the control needs to be pressed and released to perform the interaction.
        /// </summary>
        /// <remarks>
        /// If this value is equal to or smaller than zero, the input system will use (<see cref="InputSettings.defaultTapTime"/>) instead.
        /// </remarks>
        [Tooltip("The maximum time (in seconds) allowed to elapse between pressing and releasing a control for it to register as a tap.")]
        public float tapTime;

        /// <summary>
        /// The time in seconds which is allowed to pass between taps.
        /// </summary>
        /// <remarks>
        /// If this time is exceeded, the multi-tap interaction is canceled.
        /// If this value is equal to or smaller than zero, the input system will use the duplicate value of <see cref="tapTime"/> instead.
        /// </remarks>
        [Tooltip("The maximum delay (in seconds) allowed between each tap. If this time is exceeded, the multi-tap is canceled.")]
        public float tapDelay;

        /// <summary>
        /// The number of taps required to perform the interaction.
        /// </summary>
        /// <remarks>
        /// How many taps need to be performed in succession. Two means double-tap, three means triple-tap, and so on.
        /// </remarks>
        [Tooltip("How many taps need to be performed in succession. Two means double-tap, three means triple-tap, and so on.")]
        public int tapCount = 2;

        /// <summary>
        /// Magnitude threshold that must be crossed by an actuated control for the control to
        /// be considered pressed.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultButtonPressPoint"/> is used instead.
        /// </remarks>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        public float pressPoint;

        private float tapTimeOrDefault => tapTime > 0.0 ? tapTime : InputSystem.settings.defaultTapTime;
        internal float tapDelayOrDefault => tapDelay > 0.0 ? tapDelay : InputSystem.settings.multiTapDelayTime;
        private float pressPointOrDefault => pressPoint > 0 ? pressPoint : ButtonControl.s_GlobalDefaultButtonPressPoint;
        private float releasePointOrDefault => pressPointOrDefault * ButtonControl.s_GlobalDefaultButtonReleaseThreshold;

        /// <inheritdoc />
        public void Process(ref InputInteractionContext context)
        {
            if (context.timerHasExpired)
            {
                // We use timers multiple times but no matter what, if they expire it means
                // that we didn't get input in time.
                context.Canceled();
                return;
            }

            switch (m_CurrentTapPhase)
            {
                case TapPhase.None:
                    if (context.ControlIsActuated(pressPointOrDefault))
                    {
                        m_CurrentTapPhase = TapPhase.WaitingForNextRelease;
                        m_CurrentTapStartTime = context.time;
                        context.Started();

                        var maxTapTime = tapTimeOrDefault;
                        var maxDelayInBetween = tapDelayOrDefault;
                        context.SetTimeout(maxTapTime);

                        // We'll be using multiple timeouts so set a total completion time that
                        // effects the result of InputAction.GetTimeoutCompletionPercentage()
                        // such that it accounts for the total time we allocate for the interaction
                        // rather than only the time of one single timeout.
                        context.SetTotalTimeoutCompletionTime(maxTapTime * tapCount + (tapCount - 1) * maxDelayInBetween);
                    }
                    break;

                case TapPhase.WaitingForNextRelease:
                    if (!context.ControlIsActuated(releasePointOrDefault))
                    {
                        if (context.time - m_CurrentTapStartTime <= tapTimeOrDefault)
                        {
                            ++m_CurrentTapCount;
                            if (m_CurrentTapCount >= tapCount)
                            {
                                context.Performed();
                            }
                            else
                            {
                                m_CurrentTapPhase = TapPhase.WaitingForNextPress;
                                m_LastTapReleaseTime = context.time;
                                context.SetTimeout(tapDelayOrDefault);
                            }
                        }
                        else
                        {
                            context.Canceled();
                        }
                    }
                    break;

                case TapPhase.WaitingForNextPress:
                    if (context.ControlIsActuated(pressPointOrDefault))
                    {
                        if (context.time - m_LastTapReleaseTime <= tapDelayOrDefault)
                        {
                            m_CurrentTapPhase = TapPhase.WaitingForNextRelease;
                            m_CurrentTapStartTime = context.time;
                            context.SetTimeout(tapTimeOrDefault);
                        }
                        else
                        {
                            context.Canceled();
                        }
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_CurrentTapPhase = TapPhase.None;
            m_CurrentTapCount = 0;
            m_CurrentTapStartTime = 0;
            m_LastTapReleaseTime = 0;
        }

        private TapPhase m_CurrentTapPhase;
        private int m_CurrentTapCount;
        private double m_CurrentTapStartTime;
        private double m_LastTapReleaseTime;

        private enum TapPhase
        {
            None,
            WaitingForNextRelease,
            WaitingForNextPress,
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// UI that is displayed when editing <see cref="HoldInteraction"/> in the editor.
    /// </summary>
    internal class MultiTapInteractionEditor : InputParameterEditor<MultiTapInteraction>
    {
        protected override void OnEnable()
        {
            m_TapTimeSetting.Initialize("Max Tap Duration",
                "Time (in seconds) within with a control has to be released again for it to register as a tap. If the control is held "
                + "for longer than this time, the tap is canceled.",
                "Default Tap Time",
                () => target.tapTime, x => target.tapTime = x, () => InputSystem.settings.defaultTapTime);
            m_TapDelaySetting.Initialize("Max Tap Spacing",
                "The maximum delay (in seconds) allowed between each tap. If this time is exceeded, the multi-tap is canceled.",
                "Default Tap Spacing",
                () => target.tapDelay, x => target.tapDelay = x, () => InputSystem.settings.multiTapDelayTime);
            m_PressPointSetting.Initialize("Press Point",
                "The amount of actuation a control requires before being considered pressed. If not set, default to "
                + "'Default Button Press Point' in the global input settings.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v,
                () => InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
            target.tapCount = EditorGUILayout.IntField(m_TapCountLabel, target.tapCount);
            m_TapDelaySetting.OnGUI();
            m_TapTimeSetting.OnGUI();
            m_PressPointSetting.OnGUI();
        }

        private readonly GUIContent m_TapCountLabel = new GUIContent("Tap Count", "How many taps need to be performed in succession. Two means double-tap, three means triple-tap, and so on.");

        private CustomOrDefaultSetting m_PressPointSetting;
        private CustomOrDefaultSetting m_TapTimeSetting;
        private CustomOrDefaultSetting m_TapDelaySetting;
    }
    #endif
}
