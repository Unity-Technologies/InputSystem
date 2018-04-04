using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: add orientation

////TODO: reset and accumulate deltas of all touches

//// Remaining things to sort out around touch:
//// - How do we handle 'primary' touches? ATM the setup always makes touch0 the primary touch
////   by hardwiring the pointer state to it but I have doubts this is a satisfactory solution.
//// - How do we handle mouse simulation?
//// - How do we implement deltas for touch when there is no delta information from the platform?
//// - How do we implement click-detection for touch?

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Default state layout for touch devices.
    /// </summary>
    /// <remarks>
    /// Combines multiple pointers each corresponding to a finger.
    ///
    /// All fingers combine to quite a bit of state; ideally send delta events that update
    /// only specific fingers.
    /// </remarks>
    // IMPORTANT: Must match TouchInputState in native code.
    [StructLayout(LayoutKind.Explicit, Size = 360)]
    public struct TouchscreenState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('T', 'S', 'C', 'R'); }
        }

        public const int kMaxTouches = 10;

        [InputControl(layout = "Touch")]
        // Add controls compatible with what Pointer expects and redirect their
        // state to the state of touch0 so that this essentially becomes our
        // pointer control.
        // NOTE: Some controls from Pointer don't make sense for touch and we "park"
        //       them by assigning them invalid offsets (thus having automatic state
        //       layout put them at the end of our fixed state).
        [InputControl(name = "fingerId", layout = "Digital", alias = "pointerId", useStateFrom = "touch0/touchId")]
        [InputControl(name = "position", layout = "Vector2", usage = "Point", useStateFrom = "touch0/position")]
        [InputControl(name = "delta", layout = "Vector2", usage = "Secondary2DMotion", useStateFrom = "touch0/delta")]
        [InputControl(name = "pressure", layout = "Axis", usage = "Pressure", useStateFrom = "touch0/pressure")]
        [InputControl(name = "radius", layout = "Vector2", usage = "Radius", useStateFrom = "touch0/radius")]
        [InputControl(name = "phase", layout = "PointerPhase", useStateFrom = "touch0/phase")]
        [InputControl(name = "displayIndex", layout = "Digital", useStateFrom = "touch0/displayIndex")]
        [InputControl(name = "touchType", layout = "TouchType", useStateFrom = "touch0/touchType")]
        [InputControl(name = "twist", layout = "Axis", usage = "Twist", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "tilt", layout = "Vector2", usage = "Tilt", offset = InputStateBlock.kInvalidOffset)]
        ////TODO: we want to the button to be pressed when there is a primary touch
        [InputControl(name = "button", layout = "Button", usages = new[] { "PrimaryAction", "PrimaryTrigger" }, offset = InputStateBlock.kInvalidOffset)]
        [FieldOffset(0)]
        public Touch touch0;

        [InputControl(layout = "Touch")]
        [FieldOffset(36)]
        public Touch touch1;

        [InputControl(layout = "Touch")]
        [FieldOffset(72)]
        public Touch touch2;

        [InputControl(layout = "Touch")]
        [FieldOffset(108)]
        public Touch touch3;

        [InputControl(layout = "Touch")]
        [FieldOffset(144)]
        public Touch touch4;

        [InputControl(layout = "Touch")]
        [FieldOffset(180)]
        public Touch touch5;

        [InputControl(layout = "Touch")]
        [FieldOffset(216)]
        public Touch touch6;

        [InputControl(layout = "Touch")]
        [FieldOffset(252)]
        public Touch touch7;

        [InputControl(layout = "Touch")]
        [FieldOffset(288)]
        public Touch touch8;

        [InputControl(layout = "Touch")]
        [FieldOffset(324)]
        public Touch touch9;

        public unsafe Touch* touches
        {
            get
            {
                fixed(Touch * ptr = &touch0)
                {
                    return ptr;
                }
            }
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    ////REVIEW: where should be put handset vibration support? should that sit on the touchscreen class? be its own separate device?
    ////REVIEW: does it *actually* make sense to base this on Pointer?
    /// <summary>
    /// A multi-touch surface.
    /// </summary>
    [InputLayout(stateType = typeof(TouchscreenState))]
    public class Touchscreen : Pointer
    {
        /// <summary>
        /// Array of currently active touches.
        /// </summary>
        /// <remarks>
        /// This array only contains touches that are either in progress, i.e. have a phase of <see cref="PointerPhase.Began"/>
        /// or <see cref="PointerPhase.Moved"/>, or that have just ended, i.e. moved to <see cref="PointerPhase.Ended"/> or
        /// <see cref="PointerPhase.Cancelled"/> this frame.
        /// </remarks>
        public ReadOnlyArray<TouchControl> activeTouches
        {
            get
            {
                var touchCount = 0;
                bool? hadActivityThisFrame = null;
                for (var i = 0; i < allTouchControls.Count; ++i)
                {
                    // Determine whether we consider the touch "active".
                    var isActive = false;
                    var touchControl = allTouchControls[i];
                    var phaseControl = touchControl.phase;
                    var phase = phaseControl.ReadValue();
                    if (phase == PointerPhase.Began || phase == PointerPhase.Moved)
                    {
                        isActive = true;
                    }
                    else if (phase == PointerPhase.Ended || phase == PointerPhase.Cancelled)
                    {
                        // Touch has ended but we want to have it on the active list for one frame
                        // before "retiring" the touch again.
                        if (hadActivityThisFrame == null)
                            hadActivityThisFrame = device.wasUpdatedThisFrame;
                        if (hadActivityThisFrame.Value)
                        {
                            var previousPhase = phaseControl.ReadPreviousValue();
                            if (previousPhase != PointerPhase.Ended && previousPhase != PointerPhase.Cancelled)
                                isActive = true;
                        }
                    }

                    if (isActive)
                    {
                        m_TouchesArray[touchCount] = touchControl;
                        ++touchCount;
                    }
                }
                return new ReadOnlyArray<TouchControl>(m_TouchesArray, 0, touchCount);
            }
        }

        /// <summary>
        /// Array of all <see cref="TouchControl">TouchControls</see> on the device.
        /// </summary>
        /// <remarks>
        /// Will always contain <see cref="TouchscreenState.kMaxTouches"/> entries regardless of
        /// which touches (if any) are currently in progress.
        /// </remarks>
        public ReadOnlyArray<TouchControl> allTouchControls { get; private set; }

        /// <summary>
        /// The touchscreen that was added or updated last or null if there is no
        /// touchscreen connected to the system.
        /// </summary>
        public new static Touchscreen current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            var touchArray = new TouchControl[TouchscreenState.kMaxTouches];

            touchArray[0] = builder.GetControl<TouchControl>(this, "touch0");
            touchArray[1] = builder.GetControl<TouchControl>(this, "touch1");
            touchArray[2] = builder.GetControl<TouchControl>(this, "touch2");
            touchArray[3] = builder.GetControl<TouchControl>(this, "touch3");
            touchArray[4] = builder.GetControl<TouchControl>(this, "touch4");
            touchArray[5] = builder.GetControl<TouchControl>(this, "touch5");
            touchArray[6] = builder.GetControl<TouchControl>(this, "touch6");
            touchArray[7] = builder.GetControl<TouchControl>(this, "touch7");
            touchArray[8] = builder.GetControl<TouchControl>(this, "touch8");
            touchArray[9] = builder.GetControl<TouchControl>(this, "touch9");

            allTouchControls = new ReadOnlyArray<TouchControl>(touchArray);
            m_TouchesArray = new TouchControl[TouchscreenState.kMaxTouches];

            base.FinishSetup(builder);
        }

        private TouchControl[] m_TouchesArray;
    }
}
