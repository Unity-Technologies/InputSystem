using System;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// A control representing a touch contact.
    /// </summary>
    public class TouchControl : InputControl<Touch>
    {
        /// <summary>
        /// The ID of the touch contact as reported by the underlying system.
        /// </summary>
        [InputControl(alias = "pointerId", offset = 0)]
        public IntegerControl touchId { get; private set; }

        /// <summary>
        /// Absolute position on the touch surface.
        /// </summary>
        [InputControl(usage = "Point", offset = 4)]
        public Vector2Control position { get; private set; }

        [InputControl(usage = "Secondary2DMotion", offset = 12)]
        public Vector2Control delta { get; private set; }
        [InputControl(usage = "Pressure", offset = 20)]
        public AxisControl pressure { get; private set; }
        [InputControl(usage = "Radius", offset = 24)]
        public Vector2Control radius { get; private set; }
        [InputControl(format = "SHRT", offset = 32)]
        public PointerPhaseControl phase { get; private set; }
        [InputControl(format = "SBYT", offset = 34)]
        public IntegerControl displayIndex { get; private set; }
        [InputControl(format = "SBYT", offset = 35)]
        public TouchTypeControl touchType { get; private set; }

        public TouchControl()
        {
            m_StateBlock.format = new FourCC('T', 'O', 'U', 'C');
        }

        //needs to enforce layout/format

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            touchId = builder.GetControl<IntegerControl>(this, "touchId");
            position = builder.GetControl<Vector2Control>(this, "position");
            delta = builder.GetControl<Vector2Control>(this, "delta");
            pressure = builder.GetControl<AxisControl>(this, "pressure");
            radius = builder.GetControl<Vector2Control>(this, "radius");
            phase = builder.GetControl<PointerPhaseControl>(this, "phase");
            displayIndex = builder.GetControl<IntegerControl>(this, "displayIndex");
            touchType = builder.GetControl<TouchTypeControl>(this, "touchType");
            base.FinishSetup(builder);
        }

        public override unsafe Touch ReadRawValueFrom(IntPtr statePtr)
        {
            var valuePtr = (Touch*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            return *valuePtr;
        }

        protected override unsafe void WriteRawValueInto(IntPtr statePtr, Touch value)
        {
            var valuePtr = (Touch*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            UnsafeUtility.MemCpy(valuePtr, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<Touch>());
        }
    }
}
