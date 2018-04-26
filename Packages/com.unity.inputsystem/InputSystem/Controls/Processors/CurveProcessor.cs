namespace UnityEngine.Experimental.Input.Processors
{
    // Interprets a float value as time and uses it to interpolate over an
    // AnimationCurve.
    public class CurveProcessor : IInputControlProcessor<float>
    {
        public AnimationCurve curve;

        public float Process(float value, InputControl control)
        {
            return curve.Evaluate(value);
        }
    }
}
