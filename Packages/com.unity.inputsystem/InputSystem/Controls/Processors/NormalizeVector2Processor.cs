using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Normalizes a <c>Vector2</c> input value.
    /// </summary>
    /// <remarks>
    /// This processor is registered (see <see cref="InputSystem.RegisterProcessor{T}"/>) under the name "normalizeVector2".
    /// </remarks>
    /// <seealso cref="NormalizeVector3Processor"/>
    [Preserve]
    public class NormalizeVector2Processor : InputProcessor<Vector2>
    {
        /// <summary>
        /// Normalize <paramref name="value"/>. Performs the equivalent of <c>value.normalized</c>.
        /// </summary>
        /// <param name="value">Input vector.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Normalized vector.</returns>
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return value.normalized;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "NormalizeVector2()";
        }
    }
}
