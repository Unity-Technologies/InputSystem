namespace ISX
{
    // A modifier for button-like behavior. Will perform action once
    // when control is pressed and then not perform again until control
    // is released again.
    public class PressModifier : IInputActionModifier
    {
        public void Process(ref InputAction.ModifierContext context)
        {
            if (!context.isWaiting)
                return;

            var control = context.control;

            var button = control as ButtonControl;
            if (button != null)
            {
                if (button.wasJustPressed)
                    context.Performed();
            }
            else
            {
                // Essentially replicate the button press logic from ButtonControl here.

                var floatControl = control as InputControl<float>;
                if (floatControl != null)
                {
                    var value = floatControl.value;
                    var previous = floatControl.value;
                    var pressPoint = InputConfiguration.ButtonPressPoint;

                    if (previous < pressPoint && value >= pressPoint)
                        context.Performed();
                }
            }
        }

        public void Reset()
        {
        }
    }
}
