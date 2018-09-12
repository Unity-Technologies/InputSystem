using System;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// A floating-point 2D vector control composed of two <see cref="AxisControl">AxisControls</see>.
    /// </summary>
    /// <remarks>
    /// Normalization is not implied. The X and Y coordinates can be in any range or units.
    /// </remarks>
    /// <example>
    /// An example is <see cref="Pointer.position"/>.
    /// <code>
    /// Debug.Log(string.Format("Mouse position x={0} y={1}", Mouse.current.position.x.value, Mouse.current.position.y.value));
    /// </code>
    /// </example>
    public class Vector2Control : InputControl<Vector2>
    {
        /// <summary>
        /// Horizontal position of the control.
        /// </summary>
        [InputControl(offset = 0)]
        public AxisControl x { get; private set; }

        /// <summary>
        /// Vertical position of the control.
        /// </summary>
        [InputControl(offset = 4)]
        public AxisControl y { get; private set; }

        public Vector2Control()
        {
            m_StateBlock.format = InputStateBlock.kTypeVector2;
        }

        public override bool HasSignificantChange(InputEventPtr eventPtr)
        {
            Vector2 value;
            if (ReadValueFrom(eventPtr, out value))
                return Vector2.SqrMagnitude(value - ReadDefaultValue()) > float.Epsilon;
            return false;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            x = builder.GetControl<AxisControl>(this, "x");
            y = builder.GetControl<AxisControl>(this, "y");
            base.FinishSetup(builder);
        }

        public override Vector2 ReadUnprocessedValueFrom(IntPtr statePtr)
        {
            return new Vector2(x.ReadValueFrom(statePtr), y.ReadValueFrom(statePtr));
        }

        protected override void WriteUnprocessedValueInto(IntPtr statePtr, Vector2 value)
        {
            x.WriteValueInto(statePtr, value.x);
            y.WriteValueInto(statePtr, value.y);
        }
    }
}
