namespace ISX
{
    // Triggers 'Performed' if a source control, after being triggered, does not reset to its
    // default state before a specified amount of time has passed.
    public class HoldModifier : IInputActionModifier
    {
        public float duration;
        public float durationOrDefault => duration > 0.0 ? duration : InputConfiguration.HoldTime;

        private double m_TimePressed;

        public void Process(ref InputAction.Context context)
        {
            if (context.timerHasExpired)
            {
                context.Performed();
                return;
            }

            if (context.isWaiting && !context.controlHasDefaultValue)
            {
                m_TimePressed = context.time;
                context.Started();

                context.SetTimeout(durationOrDefault);
                return;
            }

            if (context.isStarted && context.controlHasDefaultValue)
            {
                if (context.time - m_TimePressed >= durationOrDefault)
                    context.Performed();
                else
                    context.Cancelled();
            }
        }

        public void Reset()
        {
            m_TimePressed = 0;
        }
    }
}
