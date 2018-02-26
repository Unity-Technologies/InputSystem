using System;
using ISX.LowLevel;
using UnityEngine;

namespace ISX.Controls
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

        protected override void FinishSetup(InputControlSetup setup)
        {
            x = setup.GetControl<AxisControl>(this, "x");
            y = setup.GetControl<AxisControl>(this, "y");
            base.FinishSetup(setup);
        }

        protected override Vector2 ReadRawValueFrom(IntPtr statePtr)
        {
            return new Vector2(x.ReadValueFrom(statePtr), y.ReadValueFrom(statePtr));
        }

        protected override void WriteRawValueInto(IntPtr statePtr, Vector2 value)
        {
            x.WriteValueInto(statePtr, value.x);
            y.WriteValueInto(statePtr, value.y);
        }
    }
}
