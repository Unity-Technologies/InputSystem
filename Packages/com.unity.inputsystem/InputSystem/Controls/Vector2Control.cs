using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A floating-point 2D vector control composed of two <see cref="AxisControl"/>s.
    /// </summary>
    /// <remarks>
    /// An example is <see cref="Pointer.position"/>.
    ///
    /// <example>
    /// <code>
    /// Debug.Log(string.Format("Mouse position x={0} y={1}",
    ///     Mouse.current.position.x.ReadValue(),
    ///     Mouse.current.position.y.ReadValue()));
    /// </code>
    ///
    /// Normalization is not implied. The X and Y coordinates can be in any range or units.
    /// </example>
    /// </remarks>
    [Scripting.Preserve]
    public class Vector2Control : InputControl<Vector2>
    {
        /// <summary>
        /// Horizontal position of the control.
        /// </summary>
        /// <value>Control representing horizontal motion input.</value>
        [InputControl(offset = 0, displayName = "X")]
        public AxisControl x { get; set; }

        /// <summary>
        /// Vertical position of the control.
        /// </summary>
        /// <value>Control representing vertical motion input.</value>
        [InputControl(offset = 4, displayName = "Y")]
        public AxisControl y { get; set; }

        /// <summary>
        /// Default-initialize the control.
        /// </summary>
        public Vector2Control()
        {
            m_StateBlock.format = InputStateBlock.FormatVector2;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            x = GetChildControl<AxisControl>("x");
            y = GetChildControl<AxisControl>("y");

            base.FinishSetup();
        }

        /// <inheritdoc />
        public override unsafe Vector2 ReadUnprocessedValueFromState(void* statePtr)
        {
            return new Vector2(
                x.ReadUnprocessedValueFromState(statePtr),
                y.ReadUnprocessedValueFromState(statePtr));
        }

        /// <inheritdoc />
        public override unsafe void WriteValueIntoState(Vector2 value, void* statePtr)
        {
            x.WriteValueIntoState(value.x, statePtr);
            y.WriteValueIntoState(value.y, statePtr);
        }

        /// <inheritdoc />
        public override unsafe float EvaluateMagnitude(void* statePtr)
        {
            ////REVIEW: this can go beyond 1; that okay?
            return ReadValueFromState(statePtr).magnitude;
        }
    }
}
