using System;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    public enum PredefinedCurve
    {
        Linear,
        Quatratic,
        Cubic,
    }

    public static class CurveExtensions
    {
        /// <summary>
        /// Apply response curve shaping to vlaue <paramref name="v"/>.
        /// </summary>
        /// <param name="v">The normalized value to be transformed.</param>
        /// <param name="curve">The response curve.</param>
        /// <returns>Transformed normalized value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="curve"/> is outside valid range.</exception>
        public static float Transform(float x, PredefinedCurve curve)
        {
            switch (curve)
            {
                case PredefinedCurve.Linear:
                    return x;
                case PredefinedCurve.Quatratic:
                    return x * x;
                case PredefinedCurve.Cubic:
                    return x * x * x;
                default:
                    throw new ArgumentOutOfRangeException(nameof(curve));
            }
        }

        /// <summary>
        /// Apply response curve shaping to vector <paramref name="v"/>.
        /// </summary>
        /// <remarks>The provided vector is first transformed to polar form, then the vector length is scaled
        /// before it is finally converted back to euclidean space.</remarks>
        /// <param name="v">A normalized Euclidean vector to be transformed.</param>
        /// <param name="curve">The response curve.</param>
        /// <returns>Transformed normalized Euclidean vector.</returns>
        public static Vector2 Transform(Vector2 v, PredefinedCurve curve)
        {
            var r = Transform(v.magnitude, curve);
            var theta = Mathf.Atan2(v.y, v.x);
            return new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
        }
    }
}
