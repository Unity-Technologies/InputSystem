using System;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Represents a response curve for mapping normalized values onto the normalized value axis.
    /// </summary>
    public enum Curve
    {
        /// <summary>
        /// Linear curve f(x) = x.
        /// </summary>
        Linear,

        /// <summary>
        /// Quadratic curve f(x) = x^2.
        /// </summary>
        Quadratic,

        /// <summary>
        /// Cubic curve f(x) = x^3.
        /// </summary>
        Cubic,
    }

    /// <summary>
    /// Extension methods for <see cref="Curve"/>.
    /// </summary>
    internal static class CurveExtensions
    {
        /// <summary>
        /// Apply response curve shaping to vlaue <paramref name="v"/>.
        /// </summary>
        /// <param name="curve">The response curve.</param>
        /// <param name="v">The normalized value to be transformed.</param>
        /// <returns>Transformed normalized value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="curve"/> is outside valid range.</exception>
        public static float Transform(this Curve curve, float v)
        {
            switch (curve)
            {
                case Curve.Linear:
                    return v;
                case Curve.Quadratic:
                    return v * v;
                case Curve.Cubic:
                    return v * v * v;
                default:
                    throw new ArgumentOutOfRangeException(nameof(curve));
            }
        }

        /// <summary>
        /// Apply response curve shaping to vector <paramref name="v"/>.
        /// </summary>
        /// <remarks>The provided vector is first transformed to polar form, then the vector length is scaled
        /// before it is finally converted back to euclidean space.</remarks>
        /// <param name="curve">The response curve.</param>
        /// <param name="v">A normalized Euclidean vector to be transformed.</param>
        /// <returns>Transformed normalized Euclidean vector.</returns>
        public static Vector2 Transform(this Curve curve, Vector2 v)
        {
            var r = curve.Transform(v.magnitude);
            var theta = Mathf.Atan2(v.y, v.x);
            return new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
        }
    }
}
