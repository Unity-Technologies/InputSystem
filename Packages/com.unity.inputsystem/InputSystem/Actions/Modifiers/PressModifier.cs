using UnityEngine.Experimental.Input.Controls;

////TODO: have ability to also observe release (separate from ReleaseModifier)

namespace UnityEngine.Experimental.Input.Modifiers
{
    // A modifier for button-like behavior. Will perform action once
    // when control is pressed and then not perform again until control
    // is released again.
    public class PressModifier : IInputBindingModifier
    {
        public void Process(ref InputAction.ModifierContext context)
        {
            if (!context.isWaiting)
                return;

            var control = context.control;

            var button = control as ButtonControl;
            if (button != null)
            {
                ////REVIEW: ths may not work as desired if multiple button state changes happen in the same update
                if (button.wasJustPressed)
                    context.Performed();
            }
            else
            {
                // Essentially replicate the button press logic from ButtonControl here.

                var floatControl = control as InputControl<float>;
                if (floatControl != null)
                {
                    var value = floatControl.ReadValue();
                    var previous = floatControl.ReadPreviousValue();
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
