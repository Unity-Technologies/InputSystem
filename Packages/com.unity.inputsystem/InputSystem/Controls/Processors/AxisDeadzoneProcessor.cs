namespace UnityEngine.Experimental.Input.Processors
{
    public class AxisDeadzoneProcessor : IInputControlProcessor<float>
    {
        public float min;
        public float max;

        public float minOrDefault
        {
            get { return min == 0.0f ? InputSystem.settings.defaultDeadzoneMin : min; }
        }

        public float maxOrDefault
        {
            get { return max == 0.0f ? InputSystem.settings.defaultDeadzoneMax : max; }
        }

        public float Process(float value, InputControl control)
        {
            var min = minOrDefault;
            var max = maxOrDefault;

            var absValue = Mathf.Abs(value);
            if (absValue < min)
                return 0;
            if (absValue > max)
                return Mathf.Sign(value);

            return Mathf.Sign(value) * ((absValue - min) / (max - min));
        }
    }
}
