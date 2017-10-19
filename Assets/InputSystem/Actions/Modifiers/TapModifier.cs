namespace ISX
{
    // Performs the action if the control is pressed and *released* within the set
    // duration (which defaults to InputConfiguration.TapTime).
    public class TapModifier : IInputActionModifier
    {
        public float duration;
        public float durationOrDefault => duration > 0.0 ? duration : InputConfiguration.TapTime;

        private double m_TapStartTime;

        public void Process(ref InputAction.ModifierContext context)
        {
            if (context.isWaiting && !context.controlHasDefaultValue)
            {
                m_TapStartTime = context.time;
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
