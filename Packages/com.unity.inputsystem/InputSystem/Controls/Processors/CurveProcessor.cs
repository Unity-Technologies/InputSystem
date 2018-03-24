using UnityEngine;

namespace UnityEngine.Experimental.Input.Processors
{
    // Interprets a float value as time and uses it to interpolate over an
    // AnimationCurve.
    public class CurveProcessor : IInputProcessor<float>
    {
        public AnimationCurve curve;

        public float Process(float value, InputControl control)
        {
            return curve.Evaluate(value);
        }
    }
}
