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
    [Scripting.Preserve]
    public class TouchControl : InputControl<TouchState>
    {
        [InputControl]
        public TouchPressControl press { get; private set; }

        /// <summary>
        /// The ID of the touch contact as reported by the underlying system.
        /// </summary>
        [InputControl]
        public IntegerControl touchId { get; private set; }

        /// <summary>
        /// Absolute position on the touch surface.
        /// </summary>
        [InputControl]
        public Vector2Control position { get; private set; }

        [InputControl]
        public Vector2Control delta { get; private set; }
        [InputControl]
        public AxisControl pressure { get; private set; }
        [InputControl]
        public Vector2Control radius { get; private set; }
        [InputControl]
        public TouchPhaseControl phase { get; private set; }
        [InputControl]
        public ButtonControl indirectTouch { get; private set; }
        [InputControl]
        public ButtonControl tap { get; private set; }
        [InputControl]
        public IntegerControl tapCount { get; private set; }
        [InputControl]
        public DoubleControl startTime { get; private set; }
        [InputControl]
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

        protected override void FinishSetup()
        {
            press = GetChildControl<TouchPressControl>("press");
            touchId = GetChildControl<IntegerControl>("touchId");
            position = GetChildControl<Vector2Control>("position");
            delta = GetChildControl<Vector2Control>("delta");
            pressure = GetChildControl<AxisControl>("pressure");
            radius = GetChildControl<Vector2Control>("radius");
            phase = GetChildControl<TouchPhaseControl>("phase");
            indirectTouch = GetChildControl<ButtonControl>("indirectTouch");
            tap = GetChildControl<ButtonControl>("tap");
            tapCount = GetChildControl<IntegerControl>("tapCount");
            startTime = GetChildControl<DoubleControl>("startTime");
            startPosition = GetChildControl<Vector2Control>("startPosition");

            ////TODO: throw if state layouts of the controls doesn't match TouchState

            base.FinishSetup();
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
