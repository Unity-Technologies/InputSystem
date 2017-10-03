using UnityEngine;

namespace ISX
{
    // Interprets a float value as time and uses it to interpolate over an
    // AnimationCurve.
    public class CurveProcessor : IInputProcessor<float>
    {
        public AnimationCurve curve;

        public float Process(float value)
        {
            return curve.Evaluate(value);
        }
    }
}
