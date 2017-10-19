namespace ISX
{
    // Performs the action if the control is pressed, held for at least the set duration
    // (which defaults to InputConfiguration.SlowTapTime) and then *released*.
    public class SlowTapModifier : IInputActionModifier
    {
        public float duration;
        public float durationOrDefault => duration > 0.0f ? duration : InputConfiguration.SlowTapTime;

        private double m_SlowTapStartTime;

        public void Process(ref InputAction.ModifierContext context)
        {
            if (context.isWaiting && !context.controlHasDefaultValue)
            {
                m_SlowTapStartTime = context.time;
                context.Started();
                return;
            }

            if (context.isStarted && context.controlHasDefaultValue)
            {
                if (context.time - m_SlowTapStartTime >= durationOrDefault)
                    context.Performed();
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
