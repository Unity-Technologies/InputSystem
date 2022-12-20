using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

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
    /// </example>
    ///
    /// Normalization is not implied. The X and Y coordinates can be in any range or units.
    /// </remarks>
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
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatVector2:
                    return *(Vector2*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
                default:
                    return new Vector2(
                        x.ReadUnprocessedValueFromStateWithCaching(statePtr),
                        y.ReadUnprocessedValueFromStateWithCaching(statePtr));
            }
        }

        /// <inheritdoc />
        public override unsafe void WriteValueIntoState(Vector2 value, void* statePtr)
        {
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatVector2:
                    *(Vector2*)((byte*)statePtr + (int)m_StateBlock.byteOffset) = value;
                    break;
                default:
                    x.WriteValueIntoState(value.x, statePtr);
                    y.WriteValueIntoState(value.y, statePtr);
                    break;
            }
        }

        /// <inheritdoc />
        public override unsafe float EvaluateMagnitude(void* statePtr)
        {
            ////REVIEW: this can go beyond 1; that okay?
            return ReadValueFromStateWithCaching(statePtr).magnitude;
        }

        protected override FourCC CalculateOptimizedControlDataType()
        {
            if (
                m_StateBlock.sizeInBits == sizeof(float) * 2 * 8 &&
                m_StateBlock.bitOffset == 0 &&
                x.optimizedControlDataType == InputStateBlock.FormatFloat &&
                y.optimizedControlDataType == InputStateBlock.FormatFloat &&
                y.m_StateBlock.byteOffset == x.m_StateBlock.byteOffset + 4
            )
                return InputStateBlock.FormatVector2;

            return InputStateBlock.FormatInvalid;
        }
    }
}
