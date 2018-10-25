using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Profiling;

////TODO: property that tells whether a Touchscreen is multi-touch capable

////TODO: property that tells whether a Touchscreen supports pressure

////TODO: if activeTouches is called multiple times in a single frame, only update the array once

////TODO: click detection / primaryaction handling

////TODO: add orientation

////TODO: look at how you can meaningfully use touches with actions

////TODO: touch is hardwired to certain memory layouts ATM; either allow flexbility or make sure the layouts cannot be changed

//// Remaining things to sort out around touch:
//// - How do we handle mouse simulation?
//// - How do we implement click-detection for touch?
//// - High frequency touches
//// - Touch prediction

namespace UnityEngine.Experimental.Input.LowLevel
{
    // IMPORTANT: Must match TouchInputState in native code.
    [StructLayout(LayoutKind.Explicit, Size = kSizeInBytes)]
    public struct TouchState : IInputStateTypeInfo
    {
        public const int kSizeInBytes = 36;

        public static FourCC kFormat
        {
            get { return new FourCC('T', 'O', 'U', 'C'); }
        }

        [InputControl(layout = "Integer")][FieldOffset(0)] public int touchId;
        [InputControl(processors = "TouchPositionTransform")][FieldOffset(4)] public Vector2 position;
        [InputControl][FieldOffset(12)] public Vector2 delta;
        [InputControl(layout = "Axis")][FieldOffset(20)] public float pressure;
        [InputControl][FieldOffset(24)] public Vector2 radius;
        [InputControl(name = "phase", layout = "PointerPhase", format = "USHT")][FieldOffset(32)] public ushort phaseId;
        [InputControl(layout = "Digital", format = "SBYT")][FieldOffset(34)] public sbyte displayIndex; ////TODO: kill this
        [InputControl(name = "indirectTouch", layout = "Button", bit = (int)TouchFlags.IndirectTouch)][FieldOffset(35)] public sbyte flags;

        public PointerPhase phase
        {
            get { return (PointerPhase)phaseId; }
            set { phaseId = (ushort)value; }
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public class TouchPositionTransformProcessor : IInputControlProcessor<Vector2>
    {
        public Vector2 Process(Vector2 value, InputControl control)
        {
#if UNITY_EDITOR
            return value;
#elif PLATFORM_ANDROID
            return new Vector2(value.x, InputRuntime.s_Instance.screenSize.y - value.y);
#else
            return value;
#endif
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
    [StructLayout(LayoutKind.Explicit, Size = kMaxTouches * TouchState.kSizeInBytes)]
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
        [InputControl(name = "pointerId", useStateFrom = "touch0/touchId")]
        [InputControl(name = "position", useStateFrom = "touch0/position")]
        [InputControl(name = "delta", useStateFrom = "touch0/delta")]
        [InputControl(name = "pressure", useStateFrom = "touch0/pressure")]
        [InputControl(name = "radius", useStateFrom = "touch0/radius")]
        [InputControl(name = "phase", useStateFrom = "touch0/phase")]
        [InputControl(name = "displayIndex", useStateFrom = "touch0/displayIndex")]
        [InputControl(name = "twist", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "tilt", offset = InputStateBlock.kInvalidOffset)]
        ////TODO: we want to the button to be pressed when there is a primary touch
        [InputControl(name = "button", offset = InputStateBlock.kInvalidOffset)]
        [FieldOffset(0)]
        public fixed byte touchData[kMaxTouches * TouchState.kSizeInBytes];

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
    [Flags]
    public enum TouchFlags
    {
        IndirectTouch
    }

    ////REVIEW: where should be put handset vibration support? should that sit on the touchscreen class? be its own separate device?
    ////REVIEW: does it *actually* make sense to base this on Pointer?
    /// <summary>
    /// A multi-touch surface.
    /// </summary>
    [InputControlLayout(stateType = typeof(TouchscreenState))]
    public class Touchscreen : Pointer, IInputStateCallbackReceiver
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
        /// or <see cref="PointerPhase.Moved"/> or <see cref="PointerPhase.Stationary"/>, or that have just ended, i.e. moved to
        /// <see cref="PointerPhase.Ended"/> or <see cref="PointerPhase.Cancelled"/> this frame.
        ///
        /// Does not allocate GC memory.
        /// </remarks>
        public ReadOnlyArray<TouchControl> activeTouches
        {
            get
            {
                var touchCount = 0;
                bool? hadActivityThisFrame = null;
                var numTouchControls = allTouchControls.Count;
                for (var i = 0; i < numTouchControls; ++i)
                {
                    // Determine whether we consider the touch "active".
                    var isActive = false;
                    var touchControl = allTouchControls[i];
                    var phaseControl = touchControl.phase;
                    var phase = phaseControl.ReadValue();
                    if (phase == PointerPhase.Began || phase == PointerPhase.Moved || phase == PointerPhase.Stationary)
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
                        m_ActiveTouchesArray[touchCount] = touchControl;
                        ++touchCount;
                    }
                }

                return new ReadOnlyArray<TouchControl>(m_ActiveTouchesArray, 0, touchCount);
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

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            var touchArray = new TouchControl[TouchscreenState.kMaxTouches];

            for (var i = 0; i < TouchscreenState.kMaxTouches; ++i)
                touchArray[i] = builder.GetControl<TouchControl>(this, "touch" + i);

            allTouchControls = new ReadOnlyArray<TouchControl>(touchArray);
            m_ActiveTouchesArray = new TouchControl[TouchscreenState.kMaxTouches];

            base.FinishSetup(builder);
        }

        ////TODO: find a better way to manage memory allocation for touches
        ////      (we really don't want to crawl through the entire state here like we do now;
        ////      whatever the solution, it'll likely be complicated by fixed vs dymamic updates)

        ////TODO: primary touch handling

        // Touch presents a somewhat more complicated picture when it comes to how to store it as state.
        //
        // We need several TouchState entries as there can be multiple touches going on at the same time.
        // However, if we give us, say, 10 TouchStates based on the assumption that a touchscreen can track
        // at most 10 concurrent touches, then for each touch we receive from the OS, we have to figure out
        // which of the TouchStates to store it in. That, however, can get a little tricky.
        //
        // We don't want to overwrite touch state before anyone had a chance to actually see it. So a touch
        // that ended in one frame should not be overwritten by a touch that started in the same frame. And
        // a touch that started and ended in the same frame should still be visible in the state for one
        // frame. This means that we can actually end up having to store information for more touches than
        // are currently in progress.
        //
        // So what we do is give us a larger pool of TouchStates to allocate from and then we decide dynamically
        // which of entry to use for a particular TouchState event. Note that this requires the runtime
        // sending us touch information not as TouchscreenState events (delta or full device) but as
        // TouchState events which in turn means that the format of incoming events ('TOUC') will not match
        // the format of the Touchscreen device state ('TSCR').
        //
        // Note that TouchManager presents an alternate API that does not have to deal with the same kind of
        // problems.
        //
        // NOTE: It is still possible to send TouchscreenState events to a Touchscreen device, just like
        //       sending state to any other device. The code here only presents an alternate path for sending
        //       state to a Touchscreen and have it perform touch allocation internally.

        unsafe bool IInputStateCallbackReceiver.OnCarryStateForward(IntPtr statePtr)
        {
            ////TODO: early out and skip crawling through touches if we didn't change state in the last update

            Profiler.BeginSample("TouchCarryStateForward");

            var haveChangedState = false;

            // Reset all touches that have ended last frame to being unused.
            // Also mark any ongoing touches as stationary.
            var touchStatePtr = (TouchState*)((byte*)statePtr.ToPointer() + stateBlock.byteOffset);
            for (var i = 0; i < TouchscreenState.kMaxTouches; ++i, ++touchStatePtr)
            {
                var phase = touchStatePtr->phase;
                switch (phase)
                {
                    case PointerPhase.Ended:
                    case PointerPhase.Cancelled:
                        touchStatePtr->phase = PointerPhase.None;
                        touchStatePtr->delta = Vector2.zero;
                        haveChangedState = true;
                        break;

                    ////REVIEW: the downside of blindly doing this here is that even if there is an upcoming
                    ////        motion event for a touch, it will briefly go stationary at the start of a frame
                    ////        (which is observable by actions)
                    case PointerPhase.Began:
                    case PointerPhase.Moved:
                        touchStatePtr->phase = PointerPhase.Stationary;
                        touchStatePtr->delta = Vector2.zero;
                        haveChangedState = true;
                        break;
                }
            }

            Profiler.EndSample();

            return haveChangedState;
        }

        unsafe bool IInputStateCallbackReceiver.OnReceiveStateWithDifferentFormat(IntPtr statePtr, FourCC stateFormat, uint stateSize,
            ref uint offsetToStoreAt)
        {
            if (stateFormat != TouchState.kFormat)
                return false;

            Profiler.BeginSample("TouchAllocate");

            // For performance reasons, we read memory here directly rather than going through
            // ReadValue() of the individual TouchControl children. This means that Touchscreen,
            // unlike other devices, is hardwired to a single memory layout only.

            var newTouchState = (TouchState*)statePtr;
            var currentTouchState = (TouchState*)((byte*)currentStatePtr.ToPointer() + stateBlock.byteOffset);

            // If it's an ongoing touch, try to find the TouchState we have allocated to the touch
            // previously.
            var phase = newTouchState->phase;
            if (phase != PointerPhase.Began)
            {
                var touchId = newTouchState->touchId;
                for (var i = 0; i < TouchscreenState.kMaxTouches; ++i, ++currentTouchState)
                {
                    if (currentTouchState->touchId == touchId)
                    {
                        offsetToStoreAt = (uint)i * TouchState.kSizeInBytes;
                        // We're going to copy the new state over the old state so update the delta
                        // on the new state to accumulate the old state.
                        newTouchState->delta = newTouchState->position - currentTouchState->position;
                        newTouchState->delta += currentTouchState->delta;
                        Profiler.EndSample();
                        return true;
                    }
                }

                // Couldn't find an entry. Either it was a touch that we previously ran out of available
                // entries for or it's an event sent out of sequence. Ignore the touch to be consistent.

                Profiler.EndSample();
                return false;
            }

            // It's a new touch. Try to find an unused TouchState.
            for (var i = 0; i < TouchscreenState.kMaxTouches; ++i, ++currentTouchState)
            {
                if (currentTouchState->phase == PointerPhase.None)
                {
                    offsetToStoreAt = (uint)i * TouchState.kSizeInBytes;
                    newTouchState->delta = Vector2.zero;
                    Profiler.EndSample();
                    return true;
                }
            }

            // We ran out of state and we don't want to stomp an existing ongoing touch.
            // Drop this touch entirely.
            Profiler.EndSample();
            return false;
        }

        void IInputStateCallbackReceiver.OnBeforeWriteNewState(IntPtr oldStatePtr, IntPtr newStatePtr)
        {
        }

        private TouchControl[] m_ActiveTouchesArray;
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
