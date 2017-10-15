namespace ISX
{
    // Triggers 'Performed' if a source control, after being triggered, does not reset to its
    // default state before a specified amount of time has passed.
    public class HoldModifier : IInputActionModifier
    {
        public float duration;

        public void Process(ref InputAction.Context context)
        {
            if (context.phase == InputAction.Phase.Waiting && !context.controlHasDefaultValue)
            {
                m_TimePressed = context.time;
                context.Started();

                var holdTime = duration;
                if (holdTime <= 0.0)
                    holdTime = InputConfiguration.HoldTime;

                context.SetTimeout(holdTime);
            }

            if (context.phase == InputAction.Phase.Started && context.controlHasDefaultValue)
            {
                if (context.time - m_TimePressed >= duration)
                    context.Performed();
                else
                    context.Cancelled();
            }
        }

        public void Reset()
        {
            m_TimePressed = 0;
        }

        private double m_TimePressed;
    }
}
