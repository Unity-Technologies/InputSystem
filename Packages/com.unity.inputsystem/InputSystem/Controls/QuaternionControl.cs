using System;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;

////REVEW: expose euler angle subcontrols?

namespace UnityEngine.Experimental.Input.Controls
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
            m_StateBlock.format = InputStateBlock.kTypeQuaternion;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            x = builder.GetControl<AxisControl>(this, "x");
            y = builder.GetControl<AxisControl>(this, "y");
            z = builder.GetControl<AxisControl>(this, "z");
            w = builder.GetControl<AxisControl>(this, "w");
            base.FinishSetup(builder);
        }

        public override Quaternion ReadUnprocessedValueFrom(IntPtr statePtr)
        {
            return new Quaternion(x.ReadValueFrom(statePtr), y.ReadValueFrom(statePtr), z.ReadValueFrom(statePtr),
                w.ReadUnprocessedValueFrom(statePtr));
        }

        protected override void WriteUnprocessedValueInto(IntPtr statePtr, Quaternion value)
        {
            x.WriteValueInto(statePtr, value.x);
            y.WriteValueInto(statePtr, value.y);
            z.WriteValueInto(statePtr, value.z);
            w.WriteValueInto(statePtr, value.w);
        }
    }
}
