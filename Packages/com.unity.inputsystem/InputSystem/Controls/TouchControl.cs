using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Scripting;

////TODO: enforce memory layout of TouchControl to be that of TouchState (build that into the layout system? "freeze"/final layout?)

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control representing a touch contact.
    /// </summary>
    /// <remarks>
    /// Note that unlike most other control types, <c>TouchControls</c> do not have
    /// a flexible memory layout. They are hardwired to <see cref="TouchState"/> and
    /// will not work correctly with a different memory layouts. Additional fields may
    /// be appended to the struct but what's there in the struct has to be located
    /// at exactly those memory addresses.
    /// </remarks>
    [InputControlLayout(stateType = typeof(TouchState))]
    [Preserve]
    public class TouchControl : InputControl<TouchState>
    {
        /// <summary>
        /// Button that indicates whether there is currently an ongoing touch
        /// contact on the control. When touch is ongoing, button will be 1,
        /// otherwise button will be 0.
        /// </summary>
        /// <value>Control representing an ongoing touch contact.</value>
        /// <remarks>
        /// This control simply monitors <see cref="phase"/> and will read as 1 whenever
        /// the phase is <see cref="TouchPhase.Began"/>, <see cref="TouchPhase.Moved"/>,
        /// or <see cref="TouchPhase.Stationary"/>.
        /// </remarks>
        /// <seealso cref="phase"/>
        public TouchPressControl press { get; private set; }

        /// <summary>
        /// The ID of the touch contact as reported by the underlying system.
        /// </summary>
        /// <value>Control reading out the ID of the touch.</value>
        /// <remarks>
        /// Each touch contact that is made with the screen receives its own unique ID which is
        /// normally assigned by the underlying platform.
        ///
        /// Note a platform may reuse touch IDs after their respective touches have finished.
        /// This means that the guarantee of uniqueness is only made with respect to currently
        /// ongoing touches.
        /// </remarks>
        /// <seealso cref="TouchState.touchId"/>
        public IntegerControl touchId { get; private set; }

        /// <summary>
        /// Absolute screen-space position on the touch surface.
        /// </summary>
        /// <value>Control representing the screen-space of the touch.</value>
        /// <seealso cref="TouchState.position"/>
        public Vector2Control position { get; private set; }

        /// <summary>
        /// Screen-space motion delta of the touch.
        /// </summary>
        /// <value>Control representing the screen-space motion delta of the touch.</value>
        /// <remarks>
        /// This is either supplied directly by the underlying platform or computed on the
        /// fly by <see cref="Touchscreen"/> from the last known position of the touch.
        ///
        /// Note that deltas have behaviors attached to them different from most other
        /// controls. See <see cref="Pointer.delta"/> for details.
        /// </remarks>
        /// <seealso cref="TouchState.delta"/>
        public Vector2Control delta { get; private set; }

        /// <summary>
        /// Normalized pressure of the touch against the touch surface.
        /// </summary>
        /// <value>Control representing the pressure level of the touch.</value>
        /// <remarks>
        /// Not all touchscreens are pressure-sensitive. If unsupported, this control will remain
        /// at default value.
        ///
        /// In general, touch pressure is supported on mobile platforms only.
        ///
        /// Note that it is possible for the value to go above 1 even though it is considered normalized. The reason is
        /// that calibration on the system can put the maximum pressure point below the physically supported maximum value.
        /// </remarks>
        /// <seealso cref="TouchState.pressure"/>
        /// <seealso cref="Pointer.pressure"/>
        public AxisControl pressure { get; private set; }

        /// <summary>
        /// Screen-space radius of the touch.
        /// </summary>
        /// <value>Control representing the horizontal and vertical extents of the touch contact.</value>
        /// <remarks>
        /// If supported by the device, this reports the size of the touch contact based on its
        /// <see cref="position"/> center point. If not supported, this will be <c>default(Vector2)</c>.
        /// </remarks>
        /// <seealso cref="Pointer.radius"/>
        public Vector2Control radius { get; private set; }

        /// <summary>
        /// Current phase of the touch.
        /// </summary>
        /// <value>Control representing the current phase of the touch.</value>
        /// <remarks>
        /// This will be <see cref="TouchPhase.None"/> if no touch has been registered on the control
        /// yet or if the control has been reset to its default state.
        /// </remarks>
        /// <seealso cref="isInProgress"/>
        public TouchPhaseControl phase { get; private set; }

        /// <summary>
        /// Whether the touch comes from a source other than direct contact with the touch surface.
        /// </summary>
        /// <value>Control indicating whether the touch was generated indirectly.</value>
        /// <remarks>
        /// Indirect touches can be generated with a stylus, for example.
        /// </remarks>
        public ButtonControl indirectTouch { get; private set; }

        /// <summary>
        /// Whether the touch has performed a tap.
        /// </summary>
        /// <value>Control that indicates whether the touch has tapped the screen.</value>
        /// <remarks>
        /// A tap is defined as a touch that begins and ends within <see cref="InputSettings.defaultTapTime"/> and
        /// stays within <see cref="InputSettings.tapRadius"/> of its <see cref="startPosition"/>. If this
        /// is the case for a touch, this button is set to 1 at the time the touch goes to <see cref="phase"/>
        /// <see cref="TouchPhase.Ended"/>.
        ///
        /// The button resets to 0 only when another touch is started on the control or when the control
        /// is reset.
        /// </remarks>
        /// <seealso cref="tapCount"/>
        /// <seealso cref="InputSettings.defaultTapTime"/>
        public ButtonControl tap { get; private set; }

        /// <summary>
        /// Number of times that the touch has been tapped in succession.
        /// </summary>
        /// <value>Control that indicates how many taps have been performed one after the other.</value>
        /// <remarks>
        /// Successive taps have to come within <see cref="InputSettings.multiTapDelayTime"/> for them
        /// to increase the tap count. I.e. if a new tap finishes within that time after <see cref="startTime"/>
        /// of the previous touch, the tap count is increased by one. If more than <see cref="InputSettings.multiTapDelayTime"/>
        /// passes after a tap with no successive tap, the tap count is reset to zero.
        /// </remarks>
        public IntegerControl tapCount { get; private set; }

        /// <summary>
        /// Time in seconds on the same timeline as <c>Time.realTimeSinceStartup</c> when the touch began.
        /// </summary>
        /// <value>Control representing the start time of the touch.</value>
        /// <remarks>
        /// This is the value of <see cref="InputEvent.time"/> when the touch starts with
        /// <see cref="phase"/> <see cref="TouchPhase.Began"/>.
        /// </remarks>
        /// <seealso cref="InputEvent.time"/>
        public DoubleControl startTime { get; private set; }

        /// <summary>
        /// Screen-space position where the touch started.
        /// </summary>
        /// <value>Control representing the start position of the touch.</value>
        /// <seealso cref="position"/>
        public Vector2Control startPosition { get; private set; }

        /// <summary>
        /// Whether a touch on the control is currently is progress.
        /// </summary>
        /// <value>If true, a touch is in progress, i.e. has a <see cref="phase"/> of
        /// <see cref="TouchPhase.Began"/>, <see cref="TouchPhase.Moved"/>, or <see
        /// cref="TouchPhase.Canceled"/>.</value>
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

        /// <summary>
        /// Default-initialize the touch control.
        /// </summary>
        /// <remarks>
        /// Sets the <see cref="InputStateBlock.format"/> to <c>"TOUC"</c>.
        /// </remarks>
        public TouchControl()
        {
            m_StateBlock.format = new FourCC('T', 'O', 'U', 'C');
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override unsafe TouchState ReadUnprocessedValueFromState(void* statePtr)
        {
            var valuePtr = (TouchState*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
            return *valuePtr;
        }

        /// <inheritdoc />
        public override unsafe void WriteValueIntoState(TouchState value, void* statePtr)
        {
            var valuePtr = (TouchState*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
            UnsafeUtility.MemCpy(valuePtr, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<TouchState>());
        }
    }
}
