using UnityEngine.Scripting;

////REVIEW: handle values dropping below min and above max?

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Normalizes input values in the range <see cref="min"/> and <see cref="max"/> to
    /// unsigned normalized form [0..1] if <see cref="zero"/> is placed at (or below) <see cref="min"/>
    /// or to signed normalized form [-1..1] if <see cref="zero"/> is placed in-between
    /// <see cref="min"/> and <see cref="max"/>.
    /// </summary>
    /// <remarks>
    /// This processor is registered (see <see cref="InputSystem.RegisterProcessor{T}"/>) under the name "normalize".
    ///
    /// Note that this processor does not clamp the incoming value to <see cref="min"/> and <see cref="max"/>.
    /// To achieve this, either add a <see cref="ClampProcessor"/> or use <see cref="AxisDeadzoneProcessor"/>
    /// which combines clamping and normalizing.
    ///
    /// <example>
    /// <code>
    /// </code>
    /// // Bind to right trigger on gamepad such that the value values below 0.3 and above 0.7 get normalized
    /// // to values between [0..1].
    /// new InputAction(binding: "&lt;Gamepad&gt;/rightTrigger", processors: "normalize(min=0.3,max=0.7)");
    /// </example>
    /// </remarks>
    /// <seealso cref="NormalizeVector2Processor"/>
    /// <seealso cref="NormalizeVector3Processor"/>
    public class NormalizeProcessor : InputProcessor<float>
    {
        /// <summary>
        /// Input value (inclusive) that corresponds to 0 or -1 (depending on <see cref="zero"/>), the lower bound.
        /// </summary>
        /// <remarks>
        /// If the input value drops below min, the result is undefined.
        /// </remarks>
        public float min;

        /// <summary>
        /// Input value (inclusive) that corresponds to 1, the upper bound.
        /// </summary>
        /// <remarks>
        /// If the input value goes beyond max, the result is undefined.
        /// </remarks>
        public float max;

        /// <summary>
        /// Input value that corresponds to 0. If this is placed at or below <see cref="min"/>, the resulting normalization
        /// returns a [0..1] value. If this is placed in-between <see cref="min"/> and <see cref="max"/>, the resulting
        /// normalization returns a [-1..1] value.
        /// </summary>
        public float zero;

        /// <summary>
        /// Normalize <paramref name="value"/> with respect to <see cref="min"/> and <see cref="max"/>.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Normalized value.</returns>
        public override float Process(float value, InputControl control)
        {
            return Normalize(value, min, max, zero);
        }

        /// <summary>
        /// Normalize <paramref name="value"/> with respect to <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="min">Lower bound. See <see cref="min"/>.</param>
        /// <param name="max">Upper bound. See <see cref="max"/>.</param>
        /// <param name="zero">Zero point. See <see cref="zero"/>.</param>
        /// <returns>Normalized value.</returns>
        /// <remarks>
        /// This method performs the same function as <see cref="Process"/>.
        /// <example>
        /// <code>
        /// // Normalize 2 against a [1..5] range. Returns 0.25.
        /// NormalizeProcessor.Normalize(2, 1, 5, 1)
        /// </code>
        /// </example>
        /// </remarks>
        public static float Normalize(float value, float min, float max, float zero)
        {
            if (zero < min)
                zero = min;
            // Prevent NaN/Inf from dividing 0 by something.
            if (Mathf.Approximately(value, min))
            {
                if (min < zero)
                    return -1f;
                return 0f;
            }
            var percentage = (value - min) / (max - min);
            if (min < zero)
                return 2 * percentage - 1;
            return percentage;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Normalize(min={min},max={max},zero={zero})";
        }
    }
}
