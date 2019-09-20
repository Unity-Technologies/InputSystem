namespace UnityEngine.InputSystem.Processors
{
    [Scripting.Preserve]
    internal class AxisDeadzoneProcessor : InputProcessor<float>
    {
        public float min;
        public float max;

        private float minOrDefault => min == default ? InputSystem.settings.defaultDeadzoneMin : min;
        private float maxOrDefault => max == default ? InputSystem.settings.defaultDeadzoneMax : max;

        public override float Process(float value, InputControl control = null)
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
