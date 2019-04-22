namespace UnityEngine.Experimental.Input.Interactions
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
                    context.Cancelled();
            }
        }

        public void Reset()
        {
            m_SlowTapStartTime = 0.0;
        }
    }
}
