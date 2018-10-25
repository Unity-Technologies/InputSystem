namespace UnityEngine.Experimental.Input.Interactions
{
    // Performs the action if the control is pressed and *released* within the set
    // duration (which defaults to InputConfiguration.TapTime).
    public class TapInteraction : IInputInteraction
    {
        public float duration;
        public float durationOrDefault
        {
            get { return duration > 0.0 ? duration : InputConfiguration.TapTime; }
        }

        private double m_TapStartTime;

        ////TODO: make sure 2d doesn't move too far

        public void Process(ref InputInteractionContext context)
        {
            if (context.timerHasExpired)
            {
                context.Cancelled();
                return;
            }

            if (context.isWaiting && !context.controlHasDefaultValue)
            {
                m_TapStartTime = context.time;
                // Set timeout slightly after duration so that if tap comes in exactly at the expiration
                // time, it still counts as a valid tap.
                context.SetTimeout(durationOrDefault + 0.00001f);
                context.Started();
                return;
            }

            if (context.isStarted && context.controlHasDefaultValue)
            {
                if (context.time - m_TapStartTime <= durationOrDefault)
                    context.Performed();
                else
                    ////REVIEW: does it matter to cancel right after expiration of 'duration' or is it enough to cancel on button up like here?
                    context.Cancelled();
            }
        }

        public void Reset()
        {
            m_TapStartTime = 0;
        }
    }
}
