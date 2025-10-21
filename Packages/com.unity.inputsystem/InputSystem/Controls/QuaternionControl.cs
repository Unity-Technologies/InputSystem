using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////REVIEW: expose euler angle subcontrols?

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A generic input control reading quaternion (rotation) values.
    /// </summary>
    public class QuaternionControl : InputControl<Quaternion>
    {
        // Accessing these components as individual controls usually doesn't make too much sense,
        // but having these controls allows changing the state format on the quaternion without
        // requiring the control to explicitly support the various different storage formats.
        // Also, it allows putting processors on the individual components which may be necessary
        // to properly convert the source data.

        /// <summary>
        /// The X component of the quaternion.
        /// </summary>
        /// <value>Control representing the X component.</value>
        [InputControl(displayName = "X")]
        public AxisControl x { get; set; }

        /// <summary>
        /// The Y component of the quaternion.
        /// </summary>
        /// <value>Control representing the Y component.</value>
        [InputControl(displayName = "Y")]
        public AxisControl y { get; set; }

        /// <summary>
        /// The Z component of the quaternion.
        /// </summary>
        /// <value>Control representing the Z component.</value>
        [InputControl(displayName = "Z")]
        public AxisControl z { get; set; }

        /// <summary>
        /// The W component of the quaternion.
        /// </summary>
        /// <value>Control representing the W component.</value>
        [InputControl(displayName = "W")]
        public AxisControl w { get; set; }

        /// <summary>
        /// Default-initialize the control.
        /// </summary>
        public QuaternionControl()
        {
            m_StateBlock.sizeInBits = sizeof(float) * 4 * 8;
            m_StateBlock.format = InputStateBlock.FormatQuaternion;
        }

        /// <inheritdoc/>
        protected override void FinishSetup()
        {
            x = GetChildControl<AxisControl>("x");
            y = GetChildControl<AxisControl>("y");
            z = GetChildControl<AxisControl>("z");
            w = GetChildControl<AxisControl>("w");
            base.FinishSetup();
        }

        /// <inheritdoc/>
        public override unsafe Quaternion ReadUnprocessedValueFromState(void* statePtr)
        {
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatQuaternion:
                    return *(Quaternion*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
                default:
                    return new Quaternion(
                        x.ReadValueFromStateWithCaching(statePtr),
                        y.ReadValueFromStateWithCaching(statePtr),
                        z.ReadValueFromStateWithCaching(statePtr),
                        w.ReadUnprocessedValueFromStateWithCaching(statePtr));
            }
        }

        /// <inheritdoc/>
        public override unsafe void WriteValueIntoState(Quaternion value, void* statePtr)
        {
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatQuaternion:
                    *(Quaternion*)((byte*)statePtr + (int)m_StateBlock.byteOffset) = value;
                    break;
                default:
                    x.WriteValueIntoState(value.x, statePtr);
                    y.WriteValueIntoState(value.y, statePtr);
                    z.WriteValueIntoState(value.z, statePtr);
                    w.WriteValueIntoState(value.w, statePtr);
                    break;
            }
        }

        protected override FourCC CalculateOptimizedControlDataType()
        {
            if (
                m_StateBlock.sizeInBits == sizeof(float) * 4 * 8 &&
                m_StateBlock.bitOffset == 0 &&
                x.optimizedControlDataType == InputStateBlock.FormatFloat &&
                y.optimizedControlDataType == InputStateBlock.FormatFloat &&
                z.optimizedControlDataType == InputStateBlock.FormatFloat &&
                w.optimizedControlDataType == InputStateBlock.FormatFloat &&
                y.m_StateBlock.byteOffset == x.m_StateBlock.byteOffset + 4 &&
                z.m_StateBlock.byteOffset == x.m_StateBlock.byteOffset + 8 &&
                w.m_StateBlock.byteOffset == x.m_StateBlock.byteOffset + 12 &&
                // For some unknown reason ReadUnprocessedValueFromState here uses ReadValueFromState on x/y/z (but not w)
                // which means we can't optimize if there any processors on x/y/z
                x.m_ProcessorStack.length == 0 &&
                y.m_ProcessorStack.length == 0 &&
                z.m_ProcessorStack.length == 0
            )
                return InputStateBlock.FormatQuaternion;

            return InputStateBlock.FormatInvalid;
        }
    }
}
