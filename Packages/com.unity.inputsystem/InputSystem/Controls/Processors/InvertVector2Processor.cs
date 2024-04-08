using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Inverts the <c>x</c> and/or <c>y</c> channel of a <c>Vector2</c>.
    /// </summary>
    /// <remarks>
    /// This process is registered (see <see cref="InputSystem.RegisterProcessor{T}"/> as "invertVector2" by default.
    ///
    /// <example>
    /// <code>
    /// // Bind to the left stick on the gamepad such that its Y channel is inverted.
    /// new InputAction(binding: "&lt;Gamepad&gt;/leftStick", processors="invertVector2(invertY,invertX=false)");
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InvertVector3Processor"/>
    public class InvertVector2Processor : InputProcessor<Vector2>
    {
        /// <summary>
        /// If true, the <c>x</c> channel of the <c>Vector2</c> input value is inverted. True by default.
        /// </summary>
        public bool invertX = true;

        /// <summary>
        /// If true, the <c>y</c> channel of the <c>Vector2</c> input value is inverted. True by default.
        /// </summary>
        public bool invertY = true;

        /// <summary>
        /// Invert the <c>x</c> and/or <c>y</c> channel of the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Vector2 with inverted channels.</returns>
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            if (invertX)
                value.x *= -1;
            if (invertY)
                value.y *= -1;
            return value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"InvertVector2(invertX={invertX},invertY={invertY})";
        }
    }
}
