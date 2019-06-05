using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

////TODO: enforce memory layout of TouchControl to be that of TouchState (build that into the layout system? "freeze"/final layout?)

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control representing a touch contact.
    /// </summary>
    [InputControlLayout(stateType = typeof(TouchState))]
    public class TouchControl : InputControl<TouchState>
    {
        public TouchPressControl press { get; private set; }

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
        public TouchPhaseControl phase { get; private set; }
        public IntegerControl displayIndex { get; private set; }
        public ButtonControl indirectTouch { get; private set; }
        public ButtonControl tap { get; private set; }
        public IntegerControl tapCount { get; private set; }
        public DoubleControl startTime { get; private set; }
        public Vector2Control startPosition { get; private set; }

        public bool isInProgress
        {
            get
            {
                switch (phase.ReadValue())
                {
                    case TouchPhase.Began:
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        return true;
                }
                return false;
            }
        }

        public TouchControl()
        {
            m_StateBlock.format = new FourCC('T', 'O', 'U', 'C');
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            press = builder.GetControl<TouchPressControl>(this, "press");
            touchId = builder.GetControl<IntegerControl>(this, "touchId");
            position = builder.GetControl<Vector2Control>(this, "position");
            delta = builder.GetControl<Vector2Control>(this, "delta");
            pressure = builder.GetControl<AxisControl>(this, "pressure");
            radius = builder.GetControl<Vector2Control>(this, "radius");
            phase = builder.GetControl<TouchPhaseControl>(this, "phase");
            displayIndex = builder.GetControl<IntegerControl>(this, "displayIndex");
            indirectTouch = builder.GetControl<ButtonControl>(this, "indirectTouch");
            tap = builder.GetControl<ButtonControl>(this, "tap");
            tapCount = builder.GetControl<IntegerControl>(this, "tapCount");
            startTime = builder.GetControl<DoubleControl>(this, "startTime");
            startPosition = builder.GetControl<Vector2Control>(this, "startPosition");

            ////TODO: throw if state layouts of the controls doesn't match TouchState

            base.FinishSetup(builder);
        }

        public override unsafe TouchState ReadUnprocessedValueFromState(void* statePtr)
        {
            var valuePtr = (TouchState*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
            return *valuePtr;
        }

        public override unsafe void WriteValueIntoState(TouchState value, void* statePtr)
        {
            var valuePtr = (TouchState*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
            UnsafeUtility.MemCpy(valuePtr, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<TouchState>());
        }
    }
}
