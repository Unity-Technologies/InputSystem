using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Scale a float value by a constant factor.
    /// </summary>
    /// <remarks>
    /// This processor is registered (see <see cref="InputSystem.RegisterProcessor{T}"/>) under the name "scale".
    ///
    /// <example>
    /// <code>
    /// </code>
    /// // Bind to left trigger on the gamepad such that its values are scaled by a factor of 2.
    /// new InputAction(binding: "&lt;Gamepad&gt;/leftTrigger", processors: "scale(factor=2)");
    /// </example>
    /// </remarks>
    /// <seealso cref="ScaleVector2Processor"/>
    /// <seealso cref="ScaleVector3Processor"/>
    public class ScaleProcessor : InputProcessor<float>
    {
        /// <summary>
        /// Scale factor to apply to incoming input values. Defaults to 1 (no scaling).
        /// </summary>
        [Tooltip("Scale factor to multiply incoming float values by.")]
        public float factor = 1;

        /// <summary>
        /// Scale the given <paramref name="value"/> by <see cref="factor"/>.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Scaled value.</returns>
        public override float Process(float value, InputControl control)
        {
            return value * factor;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Scale(factor={factor})";
        }
    }
}
