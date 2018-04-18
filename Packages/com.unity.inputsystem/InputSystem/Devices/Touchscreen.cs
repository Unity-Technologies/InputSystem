using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: add orientation

////TODO: reset and accumulate deltas of all touches

////TODO: look at how you can meaningfully use touches with actions

//// Remaining things to sort out around touch:
//// - How do we handle mouse simulation?
//// - How do we implement deltas for touch when there is no delta information from the platform?
//// - How do we implement click-detection for touch?
//// - High frequency touches
//// - Touch prediction

namespace UnityEngine.Experimental.Input.LowLevel
{
    // IMPORTANT: Must match FingerInputState in native code.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct TouchState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('T', 'O', 'U', 'C'); }
        }

        [InputControl(layout = "Integer")][FieldOffset(0)] public int touchId;
        [InputControl][FieldOffset(4)] public Vector2 position;
        [InputControl][FieldOffset(12)] public Vector2 delta;
        [InputControl(layout = "Axis")][FieldOffset(20)] public float pressure;
        [InputControl][FieldOffset(24)] public Vector2 radius;
        [InputControl(name = "phase", layout = "PointerPhase", format = "USHT")][FieldOffset(32)] public ushort phaseId;
        [InputControl(layout = "Digital", format = "SBYT")][FieldOffset(34)] public sbyte displayIndex; ////TODO: kill this
        [InputControl(name = "touchType", layout = "TouchType", format = "SBYT")][FieldOffset(35)] public sbyte touchTypeId;

        public PointerPhase phase
        {
            get { return (PointerPhase)phaseId; }
            set { phaseId = (ushort)value; }
        }

        public TouchType type
        {
            get { return (TouchType)touchTypeId; }
            set { touchTypeId = (sbyte)value; }
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    /// <summary>
    /// Default state layout for touch devices.
    /// </summary>
    /// <remarks>
    /// Combines multiple pointers each corresponding to a single contact.
    ///
    /// All touches combine to quite a bit of state; ideally send delta events that update
    /// only specific fingers.
    /// </remarks>
    // IMPORTANT: Must match TouchInputState in native code.
    [StructLayout(LayoutKind.Explicit, Size = kMaxTouches * 36)]
    public unsafe struct TouchscreenState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('T', 'S', 'C', 'R'); }
        }

        /// <summary>
        /// Maximum number of touches that can be tracked at the same time.
        /// </summary>
        /// <remarks>
        /// While most touchscreens only support a number of concurrent touches that is significantly lower
        /// than this number, having a larger pool of touch states to work with makes it possible to
        /// track short-lived touches better.
        /// </remarks>
        public const int kMaxTouches = 64;

        [InputControl(layout = "Touch", name = "touch", arraySize = kMaxTouches)]
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
        public fixed byte touchData[kMaxTouches * 36];

        public TouchState* touches
        {
            get
            {
                fixed(byte * ptr = touchData)
                {
                    return (TouchState*)ptr;
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
    public enum TouchType
    {
        Direct,
        Indirect,
        Stylus
    }

    ////REVIEW: where should be put handset vibration support? should that sit on the touchscreen class? be its own separate device?
    ////REVIEW: does it *actually* make sense to base this on Pointer?
    /// <summary>
    /// A multi-touch surface.
    /// </summary>
    [InputControlLayout(stateType = typeof(TouchscreenState))]
    public class Touchscreen : Pointer
    {
        public TouchControl primaryTouch
        {
            get { return allTouchControls[0]; }
        }

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

            for (var i = 0; i < TouchscreenState.kMaxTouches; ++i)
                touchArray[i] = builder.GetControl<TouchControl>(this, "touch" + i);

            allTouchControls = new ReadOnlyArray<TouchControl>(touchArray);
            m_TouchesArray = new TouchControl[TouchscreenState.kMaxTouches];

            base.FinishSetup(builder);
        }

        private TouchControl[] m_TouchesArray;
    }

    public class Touch
    {
        private List<TouchState> m_History;
    }

    /// <summary>
    /// Helper to make tracking of touches easier.
    /// </summary>
    /// <remarks>
    /// This class obsoletes the need to manually track touches by ID and provides
    /// various helpers such as making history data of touches available.
    /// </remarks>
    public class TouchManager
    {
        /// <summary>
        /// The amount of history kept for each single touch.
        /// </summary>
        /// <remarks>
        /// By default, this is zero meaning that no history information is kept for
        /// touches. Setting this to <c>Int32.maxValue</c> will cause all history from
        /// the beginning to the end of a touch being kept.
        /// </remarks>
        public int maxHistoryLengthPerTouch
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public Action<Touch> onTouch
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public static TouchManager instance
        {
            get { throw new NotImplementedException(); }
        }

        private Touch[] m_TouchPool;
    }

    public class TouchSimulation
    {
        public static TouchSimulation instance
        {
            get { throw new NotImplementedException(); }
        }
    }
}
