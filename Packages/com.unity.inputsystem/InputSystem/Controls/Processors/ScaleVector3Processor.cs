using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Scale the components of a <see cref="Vector3"/> by constant factors.
    /// </summary>
    /// <remarks>
    /// This processor is registered (see <see cref="InputSystem.RegisterProcessor{T}"/>) under the name "scaleVector3".
    ///
    /// <example>
    /// <code>
    /// // Double the magnitude of gravity values read from a gravity sensor.
    /// myAction.AddBinding("&lt;GravitySensor&gt;/gravity").WithProcessor("scaleVector3(x=2,y=2,z=2)");
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="ScaleProcessor"/>
    /// <seealso cref="ScaleVector2Processor"/>
    [Preserve]
    public class ScaleVector3Processor : InputProcessor<Vector3>
    {
        /// <summary>
        /// Scale factor to apply to the vector's <c>x</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming Vector3's X component by.")]
        public float x = 1;

        /// <summary>
        /// Scale factor to apply to the vector's <c>y</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming Vector3's Y component by.")]
        public float y = 1;

        /// <summary>
        /// Scale factor to apply to the vector's <c>z</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming Vector3's Z component by.")]
        public float z = 1;

        /// <summary>
        /// Return <paramref name="value"/> scaled by <see cref="x"/>, <see cref="y"/>, and <see cref="z"/>.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Scaled vector.</returns>
        public override Vector3 Process(Vector3 value, InputControl control)
        {
            return new Vector3(value.x * x, value.y * y, value.z * z);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ScaleVector3(x={x},y={y},z={z})";
        }
    }
}
