using System;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// A floating-point 3D vector control composed of three <see cref="AxisControl">AxisControls</see>.
    /// </summary>
    public class Vector3Control : InputControl<Vector3>
    {
        [InputControl(offset = 0)]
        public AxisControl x { get; private set; }
        [InputControl(offset = 4)]
        public AxisControl y { get; private set; }
        [InputControl(offset = 8)]
        public AxisControl z { get; private set; }

        public Vector3Control()
        {
            m_StateBlock.format = InputStateBlock.kTypeVector3;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            x = builder.GetControl<AxisControl>(this, "x");
            y = builder.GetControl<AxisControl>(this, "y");
            z = builder.GetControl<AxisControl>(this, "z");
            base.FinishSetup(builder);
        }

        public override Vector3 ReadUnprocessedValueFrom(IntPtr statePtr)
        {
            return new Vector3(x.ReadValueFrom(statePtr), y.ReadValueFrom(statePtr), z.ReadValueFrom(statePtr));
        }

        protected override void WriteUnprocessedValueInto(IntPtr statePtr, Vector3 value)
        {
            x.WriteValueInto(statePtr, value.x);
            y.WriteValueInto(statePtr, value.y);
            z.WriteValueInto(statePtr, value.z);
        }
    }
}
