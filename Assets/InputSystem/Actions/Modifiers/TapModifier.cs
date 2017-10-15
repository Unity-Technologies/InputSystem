namespace ISX
{
    // Requires the triggering control to press and release in less than
    // the set duration (which defaults to InputConfiguration.ClickTime).
    public class TapModifier : IInputActionModifier
    {
        public float duration;
        public float durationOrDefault => duration > 0.0 ? duration : InputConfiguration.TapTime;

        private double m_TapStartTime;

        public void Process(ref InputAction.Context context)
        {
            if (context.isWaiting && !context.controlHasDefaultValue)
            {
                m_TapStartTime = context.time;
                context.Started();
                return;
            }

            if (context.isStarted && context.controlHasDefaultValue)
            {
                if (context.time - m_TapStartTime <= m_TapStartTime)
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
