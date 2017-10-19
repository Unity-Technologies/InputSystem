namespace ISX
{
    // Performs the action if the control is pressed and *held* for at least the
    // set duration (which defaults to InputConfiguration.HoldTime).
    public class HoldModifier : IInputActionModifier
    {
        public float duration;
        public float durationOrDefault => duration > 0.0 ? duration : InputConfiguration.HoldTime;

        private double m_TimePressed;
        private InputControl m_PressedControl;

        public void Process(ref InputAction.ModifierContext context)
        {
            if (context.timerHasExpired)
            {
                context.Performed();
                return;
            }

            if (context.isWaiting && !context.controlHasDefaultValue)
            {
                m_TimePressed = context.time;
                m_PressedControl = context.control;

                context.Started();
                context.SetTimeout(durationOrDefault);

                return;
            }

            ////TODO: need to ignore releases on controls that aren't m_PressedControl
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
            m_PressedControl = null;
        }
    }
}
