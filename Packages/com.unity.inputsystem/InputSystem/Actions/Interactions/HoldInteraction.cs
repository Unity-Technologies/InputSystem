namespace UnityEngine.Experimental.Input.Interactions
{
    // Performs the action if the control is pressed and *held* for at least the
    // set duration (which defaults to InputConfiguration.HoldTime).
    public class HoldInteraction : IInputInteraction
    {
        public float duration;
        public float durationOrDefault
        {
            get { return duration > 0.0 ? duration : InputConfiguration.HoldTime; }
        }

        // If true, the action will be performed repeatedly every 'duration'
        // intervals for as long as a control is pressed.
        public bool repeat;

        private double m_TimePressed;

        public void Process(ref InputInteractionContext context)
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
        }
    }
}
