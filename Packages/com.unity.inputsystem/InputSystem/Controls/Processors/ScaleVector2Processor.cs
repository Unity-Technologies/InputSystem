namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Scale the components of a <see cref="Vector2"/> by constant factors.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code>
    /// // Double the length of the vector produced by leftStick on gamepad.
    /// myAction.AddBinding("&lt;Gamepad&gt;/leftStick").WithProcessor("scaleVector2(x=2,y=2)");
    /// </code>
    /// </example>
    /// </remarks>
    [Scripting.Preserve]
    public class ScaleVector2Processor : InputProcessor<Vector2>
    {
        [Tooltip("Scale factor to multiple the incoming Vector2's X component by.")]
        public float x = 1;

        [Tooltip("Scale factor to multiple the incoming Vector2's Y component by.")]
        public float y = 1;

        /// <inheritdoc />
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return new Vector2(value.x * x, value.y * y);
        }
    }
}
