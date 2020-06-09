using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Normalizes a <c>Vector3</c> input value.
    /// </summary>
    /// <remarks>
    /// This processor is registered (see <see cref="InputSystem.RegisterProcessor{T}"/>) under the name "normalizeVector3".
    /// </remarks>
    /// <seealso cref="NormalizeVector2Processor"/>
    [Preserve]
    public class NormalizeVector3Processor : InputProcessor<Vector3>
    {
        /// <summary>
        /// Normalize <paramref name="value"/>. Performs the equivalent of <c>value.normalized</c>.
        /// </summary>
        /// <param name="value">Input vector.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Normalized vector.</returns>
        public override Vector3 Process(Vector3 value, InputControl control)
        {
            return value.normalized;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "NormalizeVector3()";
        }
    }
}
