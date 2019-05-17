#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if the control is pressed and held for at least the
    /// set duration (which defaults to <see cref="InputSettings.defaultHoldTime"/>).
    /// </summary>
    public class HoldInteraction : IInputInteraction
    {
        /// <summary>
        /// Duration in seconds that the control must be pressed for the hold to register.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultHoldTime"/> is used.
        /// </remarks>
        public float duration;

        /// <summary>
        /// Magnitude threshold that must be crossed by an actuated control for the control to
        /// be considered pressed.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultButtonPressPoint"/> is used.
        /// </remarks>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        public float pressPoint;

        /// <summary>
        /// If enabled, then while the hold is in <see cref="InputActionPhase.Started"/> phase, <see cref="InputAction.started"/>
        /// will be triggered every time a bound control changes value.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public bool startContinuously;

        private float durationOrDefault => duration > 0.0 ? duration : InputSystem.settings.defaultHoldTime;
        private float pressPointOrDefault => pressPoint > 0.0 ? pressPoint : InputSystem.settings.defaultButtonPressPoint;

        private double m_TimePressed;

        public void Process(ref InputInteractionContext context)
        {
            if (context.timerHasExpired)
            {
                if (context.continuous)
                    context.PerformedAndStayPerformed();
                else
                    context.PerformedAndGoBackToWaiting();
                return;
            }

            switch (context.phase)
            {
                case InputActionPhase.Waiting:
                    if (context.ControlIsActuated(pressPointOrDefault))
                    {
                        m_TimePressed = context.time;

                        context.Started();
                        context.SetTimeout(durationOrDefault);
                    }
                    break;

                case InputActionPhase.Started:
                    // If we've reached our hold time threshold, perform the hold.
                    // We do this regardless of what state the control changed to.
                    if (context.time - m_TimePressed >= durationOrDefault)
                    {
                        context.PerformedAndStayPerformed();
                    }
                    else if (!context.ControlIsActuated())
                    {
                        // Control is no longer actuated and we haven't performed a hold yet,
                        // so cancel.
                        context.Canceled();
                    }
                    break;

                case InputActionPhase.Performed:
                    if (context.ControlIsActuated(pressPointOrDefault))
                    {
                        if (context.continuous)
                            context.PerformedAndStayPerformed();
                    }
                    else
                    {
                        context.Canceled();
                    }
                    break;
            }
        }

        public void Reset()
        {
            m_TimePressed = 0;
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// UI that is displayed when editing <see cref="HoldInteraction"/> in the editor.
    /// </summary>
    internal class HoldInteractionEditor : InputParameterEditor<HoldInteraction>
    {
        protected override void OnEnable()
        {
            m_PressPointSetting.Initialize("Press Point",
                "Float value that an axis control has to cross for it to be considered pressed.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v, () => InputSystem.settings.defaultButtonPressPoint);
            m_DurationSetting.Initialize("Hold Time",
                "Time (in seconds) that a control has to be held in order for it to register as a hold.",
                "Default Hold Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultHoldTime);
        }

        public override void OnGUI()
        {
            //target.startContinuously = GUILayout.Toggle(target.startContinuously, m_ContinuousStartsLabel);

            m_PressPointSetting.OnGUI();
            m_DurationSetting.OnGUI();
        }

        private GUIContent m_ContinuousStartsLabel = new GUIContent("Start Continuously",
            "If enabled, the Hold will triggered 'started' repeatedly for as long as ");
        private CustomOrDefaultSetting m_PressPointSetting;
        private CustomOrDefaultSetting m_DurationSetting;
    }
    #endif
}
