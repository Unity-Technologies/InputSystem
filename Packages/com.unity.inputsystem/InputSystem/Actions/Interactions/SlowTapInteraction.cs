#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    // Performs the action if the control is pressed, held for at least the set duration
    // (which defaults to InputSettings.defaultSlowTapTime) and then *released*.
    public class SlowTapInteraction : IInputInteraction
    {
        public float duration;
        public float pressPoint;

        ////REVIEW: this seems stupid; shouldn't a slow tap just be anything that takes longer than TapTime?
        private float durationOrDefault => duration > 0.0f ? duration : InputSystem.settings.defaultSlowTapTime;
        private float pressPointOrDefault => pressPoint > 0 ? pressPoint : InputSystem.settings.defaultButtonPressPoint;

        // If this is non-zero, then if the control is held for longer than
        // this time, the slow tap is not performed when the control is finally
        // released.
        //public float expiresAfter;////TODO

        private double m_SlowTapStartTime;

        public void Process(ref InputInteractionContext context)
        {
            if (context.isWaiting && context.ControlIsActuated(pressPointOrDefault))
            {
                m_SlowTapStartTime = context.time;
                context.Started();
                return;
            }

            if (context.isStarted && !context.ControlIsActuated(pressPointOrDefault))
            {
                if (context.time - m_SlowTapStartTime >= durationOrDefault)
                    context.PerformedAndGoBackToWaiting();
                else
                    ////REVIEW: does it matter to cancel right after expiration of 'duration' or is it enough to cancel on button up like here?
                    context.Canceled();
            }
        }

        public void Reset()
        {
            m_SlowTapStartTime = 0.0;
        }
    }

    #if UNITY_EDITOR
    internal class SlowTapInteractionEditor : InputParameterEditor<SlowTapInteraction>
    {
        protected override void OnEnable()
        {
            m_DurationSetting.Initialize("Min Tap Duration",
                "Minimum time (in seconds) that a control has to be held for it to register as a slow tap. If the control is released "
                + "before this time, the slow tap is canceled.",
                "Default Slow Tap Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultSlowTapTime);
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
