using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Inverts the <c>x</c> and/or <c>y</c> and/or <c>z</c> channel of a <c>Vector3</c>.
    /// </summary>
    /// <remarks>
    /// This process is registered (see <see cref="InputSystem.RegisterProcessor{T}"/> as "invertVector3" by default.
    ///
    /// <example>
    /// <code>
    /// // Bind to gravity sensor such that its Y value is inverted.
    /// new InputAction(binding: "&lt;GravitySensor&gt;/gravity", processors="invertVector3(invertX=false,invertY,invertZ=false)");
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InvertVector2Processor"/>
    [Preserve]
    public class InvertVector3Processor : InputProcessor<Vector3>
    {
        /// <summary>
        /// If true, the <c>x</c> channel of the <c>Vector3</c> input value is inverted. True by default.
        /// </summary>
        public bool invertX = true;

        /// <summary>
        /// If true, the <c>y</c> channel of the <c>Vector3</c> input value is inverted. True by default.
        /// </summary>
        public bool invertY = true;

        /// <summary>
        /// If true, the <c>z</c> channel of the <c>Vector3</c> input value is inverted. True by default.
        /// </summary>
        public bool invertZ = true;

        /// <summary>
        /// Return the given vector with the respective channels being inverted.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Vector with channels inverted according to <see cref="invertX"/>, <see cref="invertY"/>, and <see cref="invertZ"/>.</returns>
        public override Vector3 Process(Vector3 value, InputControl control)
        {
            if (invertX)
                value.x *= -1;
            if (invertY)
                value.y *= -1;
            if (invertZ)
                value.z *= -1;
            return value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"InvertVector3(invertX={invertX},invertY={invertY},invertZ={invertZ})";
        }
    }
}
