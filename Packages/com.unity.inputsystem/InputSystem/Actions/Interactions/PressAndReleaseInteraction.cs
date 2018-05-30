namespace UnityEngine.Experimental.Input.Interactions
{
    public class PressAndReleaseInteraction : IInputInteraction
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
            var control = context.control;
            var floatControl = control as InputControl<float>;
            if (floatControl == null)
                return;

            var value = floatControl.ReadValue();
            ////FIXME: we want the previously stored value here, not the value from the previous frame
            var previous = floatControl.ReadPreviousValue();
            var threshold = pressPointOrDefault;

            if (context.isWaiting)
            {
                if (previous < threshold && value >= threshold)
                    context.Started();
            }
            else
            {
                if (previous >= threshold && value < threshold)
                    context.Performed();
            }
        }

        public void Reset()
        {
        }
    }
}
