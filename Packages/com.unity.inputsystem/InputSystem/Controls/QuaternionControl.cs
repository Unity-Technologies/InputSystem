using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

////REVIEW: expose euler angle subcontrols?

namespace UnityEngine.InputSystem.Controls
{
    public class QuaternionControl : InputControl<Quaternion>
    {
        // Accessing these components as individual controls usually doesn't make too much sense,
        // but having these controls allows changing the state format on the quaternion without
        // requiring the control to explicitly support the various different storage formats.
        // Also, it allows putting processors on the individual components which may be necessary
        // to properly convert the source data.

        public AxisControl x { get; private set; }
        public AxisControl y { get; private set; }
        public AxisControl z { get; private set; }
        public AxisControl w { get; private set; }

        public QuaternionControl()
        {
            m_StateBlock.sizeInBits = sizeof(float) * 4 * 8;
            m_StateBlock.format = InputStateBlock.FormatQuaternion;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            x = builder.GetControl<AxisControl>(this, "x");
            y = builder.GetControl<AxisControl>(this, "y");
            z = builder.GetControl<AxisControl>(this, "z");
            w = builder.GetControl<AxisControl>(this, "w");
            base.FinishSetup(builder);
        }

        public override unsafe Quaternion ReadUnprocessedValueFromState(void* statePtr)
        {
            return new Quaternion(x.ReadValueFromState(statePtr), y.ReadValueFromState(statePtr), z.ReadValueFromState(statePtr),
                w.ReadUnprocessedValueFromState(statePtr));
        }

        public override unsafe void WriteValueIntoState(Quaternion value, void* statePtr)
        {
            x.WriteValueIntoState(value.x, statePtr);
            y.WriteValueIntoState(value.y, statePtr);
            z.WriteValueIntoState(value.z, statePtr);
            w.WriteValueIntoState(value.w, statePtr);
        }
    }
}
