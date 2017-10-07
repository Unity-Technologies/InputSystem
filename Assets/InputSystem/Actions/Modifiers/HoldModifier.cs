namespace ISX
{
    ////REVIEW: this should also work on triggers! maybe use InputControl<float> as target?
    // Triggers 'Performed' if a source control, after being triggered, does not reset to its
    // default state before a specified amount of time has passed.
    public class HoldModifier : InputActionModifier<ButtonControl>
    {
        public float duration;

        ////REVIEW: this doesn't actually work... the hold must be triggered when there is *no* change in value
        ////        for a certain time -- something actions cannot actually do ATM

        public override InputAction.Phase ProcessValueChange(InputAction action, ButtonControl control, double time)
        {
            if (action.phase == InputAction.Phase.Waiting && control.wasPressedThisFrame)
            {
                m_TimePressed = time;
                return InputAction.Phase.Started;
            }
            if (action.phase == InputAction.Phase.Started && control.wasReleasedThisFrame)
            {
                if (time - m_TimePressed >= duration)
                    return InputAction.Phase.Performed;
                return InputAction.Phase.Cancelled;
            }

            return action.phase;
        }

        private double m_TimePressed;
    }
}
