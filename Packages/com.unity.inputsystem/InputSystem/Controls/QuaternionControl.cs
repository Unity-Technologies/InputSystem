using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

////REVIEW: expose euler angle subcontrols?

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A generic input control reading quaternion (rotation) values.
    /// </summary>
    [Scripting.Preserve]
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
        public AxisControl x { get; private set; }

        /// <summary>
        /// The Y component of the quaternion.
        /// </summary>
        /// <value>Control representing the Y component.</value>
        [InputControl(displayName = "Y")]
        public AxisControl y { get; private set; }

        /// <summary>
        /// The Z component of the quaternion.
        /// </summary>
        /// <value>Control representing the Z component.</value>
        [InputControl(displayName = "Z")]
        public AxisControl z { get; private set; }

        /// <summary>
        /// The W component of the quaternion.
        /// </summary>
        /// <value>Control representing the W component.</value>
        [InputControl(displayName = "W")]
        public AxisControl w { get; private set; }

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
            return new Quaternion(x.ReadValueFromState(statePtr), y.ReadValueFromState(statePtr), z.ReadValueFromState(statePtr),
                w.ReadUnprocessedValueFromState(statePtr));
        }

        /// <inheritdoc/>
        public override unsafe void WriteValueIntoState(Quaternion value, void* statePtr)
        {
            x.WriteValueIntoState(value.x, statePtr);
            y.WriteValueIntoState(value.y, statePtr);
            z.WriteValueIntoState(value.z, statePtr);
            w.WriteValueIntoState(value.w, statePtr);
        }
    }
}
