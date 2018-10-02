using System;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;

////TODO: enforce memory layout of TouchControl to be in TouchState.kFormat

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// A control representing a touch contact.
    /// </summary>
    [InputControlLayout(stateType = typeof(TouchState))]
    public class TouchControl : InputControl<TouchState>
    {
        /// <summary>
        /// The ID of the touch contact as reported by the underlying system.
        /// </summary>
        public IntegerControl touchId { get; private set; }

        /// <summary>
        /// Absolute position on the touch surface.
        /// </summary>
        public Vector2Control position { get; private set; }

        public Vector2Control delta { get; private set; }
        public AxisControl pressure { get; private set; }
        public Vector2Control radius { get; private set; }
        public PointerPhaseControl phase { get; private set; }
        public IntegerControl displayIndex { get; private set; }
        public ButtonControl indirectTouch { get; private set; }

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
            indirectTouch = builder.GetControl<ButtonControl>(this, "indirectTouch");
            base.FinishSetup(builder);
        }

        public override unsafe TouchState ReadUnprocessedValueFrom(IntPtr statePtr)
        {
            var valuePtr = (TouchState*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            return *valuePtr;
        }

        protected override unsafe void WriteUnprocessedValueInto(IntPtr statePtr, TouchState value)
        {
            var valuePtr = (TouchState*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            UnsafeUtility.MemCpy(valuePtr, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<TouchState>());
        }
    }
}
