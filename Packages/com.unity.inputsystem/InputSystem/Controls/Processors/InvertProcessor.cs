using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// An input processor that inverts its input value.
    /// </summary>
    /// <remarks>
    /// This process is registered (see <see cref="InputSystem.RegisterProcessor{T}"/> as "invert" by default.
    ///
    /// <example>
    /// <code>
    /// // Bind to the gamepad's left trigger such that it returns inverted values.
    /// new InputAction(binding: "&lt;Gamepad&gt;/leftTrigger", processors="invert");
    /// </code>
    /// </example>
    /// </remarks>
    [Preserve]
    public class InvertProcessor : InputProcessor<float>
    {
        /// <summary>
        /// Return the inverted value of <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Invert value.</returns>
        public override float Process(float value, InputControl control)
        {
            return value * -1.0f;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Invert()";
        }
    }
}
