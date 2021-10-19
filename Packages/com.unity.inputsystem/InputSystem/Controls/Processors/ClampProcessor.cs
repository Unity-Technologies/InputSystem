using UnityEngine.Scripting;

////TODO: move clamping settings into struct and add process function; then embed both here and in AxisControl

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Clamp a floating-point input to between <see cref="min"/> and <see cref="max"/>. This is equivalent
    /// to <c>Mathf.Clamp(value, min, max)</c>.
    /// </summary>
    /// <remarks>
    /// This processor is registered (see <see cref="InputSystem.RegisterProcessor{T}"/>) under the name "clamp" by default.
    ///
    /// Note that no normalization is performed. If you want to re-normalize the input value after clamping,
    /// add a <see cref="NormalizeProcessor"/>. Alternatively, add a <see cref="AxisDeadzoneProcessor"/> which
    /// both clamps and normalizes.
    ///
    /// <example>
    /// <code>
    /// </code>
    /// // Bind to right trigger on gamepad such that the value never drops below 0.3 and never goes
    /// // above 0.7.
    /// new InputAction(binding: "&lt;Gamepad&gt;/rightTrigger", processors: "clamp(min=0.3,max=0.7)");
    /// </example>
    /// </remarks>
    public class ClampProcessor : InputProcessor<float>
    {
        /// <summary>
        /// Minimum value (inclusive!) of the accepted value range.
        /// </summary>
        public float min;

        /// <summary>
        /// Maximum value (inclusive!) of the accepted value range.
        /// </summary>
        public float max;

        /// <summary>
        /// Clamp <paramref name="value"/> to the range of <see cref="min"/> and <see cref="max"/>.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Clamped value.</returns>
        public override float Process(float value, InputControl control)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Clamp(min={min},max={max})";
        }
    }
}
