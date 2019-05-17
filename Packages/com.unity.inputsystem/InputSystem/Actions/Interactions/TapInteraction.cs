#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if the control is pressed and released within the set
    /// duration (which defaults to <see cref="InputSettings.defaultTapTime"/>).
    /// </summary>
    public class TapInteraction : IInputInteraction
    {
        public float duration;
        public float pressPoint;

        private float durationOrDefault => duration > 0.0 ? duration : InputSystem.settings.defaultTapTime;
        private float pressPointOrDefault => pressPoint > 0 ? pressPoint : InputSystem.settings.defaultButtonPressPoint;

        private double m_TapStartTime;

        ////TODO: make sure 2d doesn't move too far

        public void Process(ref InputInteractionContext context)
        {
            if (context.timerHasExpired)
            {
                context.Canceled();
                return;
            }

            if (context.isWaiting && context.ControlIsActuated(pressPointOrDefault))
            {
                m_TapStartTime = context.time;
                // Set timeout slightly after duration so that if tap comes in exactly at the expiration
                // time, it still counts as a valid tap.
                context.SetTimeout(durationOrDefault + 0.00001f);
                context.Started();
                return;
            }

            if (context.isStarted && !context.ControlIsActuated(pressPointOrDefault))
            {
                if (context.time - m_TapStartTime <= durationOrDefault)
                {
                    context.PerformedAndGoBackToWaiting();
                }
                else
                {
                    ////REVIEW: does it matter to cancel right after expiration of 'duration' or is it enough to cancel on button up like here?
                    context.Canceled();
                }
            }
        }

        public void Reset()
        {
            m_TapStartTime = 0;
        }
    }

    #if UNITY_EDITOR
    internal class TapInteractionEditor : InputParameterEditor<TapInteraction>
    {
        protected override void OnEnable()
        {
            m_DurationSetting.Initialize("Max Tap Duration",
                "Time (in seconds) within with a control has to be released again for it to register as a tap. If the control is held "
                + "for longer than this time, the tap is canceled.",
                "Default Tap Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultTapTime);
            m_PressPointSetting.Initialize("Press Point",
                "The amount of actuation a control requires before being considered pressed. If not set, default to "
                + "'Default Button Press Point' in the global input settings.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v,
                () => InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
            m_DurationSetting.OnGUI();
            m_PressPointSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_DurationSetting;
        private CustomOrDefaultSetting m_PressPointSetting;
    }
    #endif
}
