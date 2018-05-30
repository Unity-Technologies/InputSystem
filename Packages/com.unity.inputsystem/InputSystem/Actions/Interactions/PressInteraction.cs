////TODO: have ability to also observe release (separate from ReleaseInteraction)

namespace UnityEngine.Experimental.Input.Interactions
{
    // A interaction for button-like behavior. Will perform action once
    // when control is pressed and then not perform again until control
    // is released again.
    public class PressInteraction : IInputInteraction
    {
        public float pressPoint;

        private float pressPointOrDefault
        {
            get
            {
                if (pressPoint > 0)
                    return pressPoint;
                return InputConfiguration.ButtonPressPoint;
            }
        }

        public void Process(ref InputInteractionContext context)
        {
            if (!context.isWaiting)
                return;

            var control = context.control;
            var floatControl = control as InputControl<float>;
            if (floatControl == null)
                return;

            var value = floatControl.ReadValue();
            ////FIXME: we want the previously stored value here, not the value from the previous frame
            var previous = floatControl.ReadPreviousValue();
            var threshold = pressPointOrDefault;

            if (previous < threshold && value >= threshold)
                context.Performed();
        }

        public void Reset()
        {
        }
    }
}
