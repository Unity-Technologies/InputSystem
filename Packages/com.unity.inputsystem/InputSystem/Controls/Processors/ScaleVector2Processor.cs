namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Scale the components of a <see cref="Vector2"/> by constant factors.
    /// </summary>
    /// <remarks>
    /// This processor is registered (see <see cref="InputSystem.RegisterProcessor{T}"/>) under the name "scaleVector2".
    ///
    /// <example>
    /// <code>
    /// // Double the length of the vector produced by leftStick on gamepad.
    /// myAction.AddBinding("&lt;Gamepad&gt;/leftStick").WithProcessor("scaleVector2(x=2,y=2)");
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="ScaleProcessor"/>
    /// <seealso cref="ScaleVector3Processor"/>
    [Scripting.Preserve]
    public class ScaleVector2Processor : InputProcessor<Vector2>
    {
        /// <summary>
        /// Scale factor to apply to the vector's <c>x</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming Vector2's X component by.")]
        public float x = 1;

        /// <summary>
        /// Scale factor to apply to the vector's <c>y</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming Vector2's Y component by.")]
        public float y = 1;

        /// <summary>
        /// Return <paramref name="value"/> scaled by <see cref="x"/> and <see cref="y"/>.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Scaled vector.</returns>
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return new Vector2(value.x * x, value.y * y);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ScaleVector2(x={x},y={y})";
        }
    }
}
