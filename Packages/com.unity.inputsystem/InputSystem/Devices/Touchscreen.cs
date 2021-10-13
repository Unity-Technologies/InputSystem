using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Profiling;

////TODO: property that tells whether a Touchscreen is multi-touch capable

////TODO: property that tells whether a Touchscreen supports pressure

////TODO: add support for screen orientation

////TODO: touch is hardwired to certain memory layouts ATM; either allow flexibility or make sure the layouts cannot be changed

////TODO: startTimes are baked *external* times; reset touch when coming out of play mode

////TODO: detect and diagnose touchId=0 events

////REVIEW: where should we put handset vibration support? should that sit on the touchscreen class? be its own separate device?

////REVIEW: Given that Touchscreen is no use for polling, should we remove Touchscreen.current?

////REVIEW: Should Touchscreen reset individual TouchControls to default(TouchState) after a touch has ended? This would allow
////        binding to a TouchControl as a whole and the action would correctly cancel if the touch ends

namespace UnityEngine.InputSystem.LowLevel
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "byte to correspond to TouchState layout.")]
    [Flags]
    internal enum TouchFlags : byte
    {
        IndirectTouch = 1 << 0,

        // NOTE: Leaving the first 3 bits for native.

        PrimaryTouch = 1 << 3,
        TapPress = 1 << 4,
        TapRelease = 1 << 5,

        // Indicates that the touch that established this primary touch has ended but that when
        // it did, there were still other touches going on. We end the primary touch when the
        // last touch leaves the screen.
        OrphanedPrimaryTouch = 1 << 6,

        // This is only used by EnhancedTouch to mark touch records that have begun in the same
        // frame as the current touch record.
        BeganInSameFrame = 1 << 7,
    }

    ////REVIEW: add timestamp directly to touch?
    /// <summary>
    /// State layout for a single touch.
    /// </summary>
    /// <remarks>
    /// This is the low-level memory representation of a single touch, i.e the
    /// way touches are internally transmitted and stored in the system. To update
    /// touches on a <see cref="Touchscreen"/>, <see cref="StateEvent"/>s containing
    /// TouchStates are sent to the screen.
    /// </remarks>
    /// <seealso cref="TouchControl"/>
    /// <seealso cref="Touchscreen"/>
    // IMPORTANT: Must match TouchInputState in native code.
    [StructLayout(LayoutKind.Explicit, Size = kSizeInBytes)]
    public struct TouchState : IInputStateTypeInfo
    {
        internal const int kSizeInBytes = 56;

        /// <summary>
        /// Memory format tag for TouchState.
        /// </summary>
        /// <value>Returns "TOUC".</value>
        /// <seealso cref="InputStateBlock.format"/>
        public static FourCC Format => new FourCC('T', 'O', 'U', 'C');

        ////REVIEW: this should really be a uint
        /// <summary>
        /// Numeric ID of the touch.
        /// </summary>
        /// <value>Numeric ID of the touch.</value>
        /// <remarks>
        /// While a touch is ongoing, it must have a non-zero ID different from
        /// all other ongoing touches. Starting with <see cref="TouchPhase.Began"/>
        /// and ending with <see cref="TouchPhase.Ended"/> or <see cref="TouchPhase.Canceled"/>,
        /// a touch is identified by its ID, i.e. a TouchState with the same ID
        /// belongs to the same touch.
        ///
        /// After a touch has ended or been canceled, an ID can be reused.
        /// </remarks>
        /// <seealso cref="TouchControl.touchId"/>
        [InputControl(displayName = "Touch ID", layout = "Integer", synthetic = true, dontReset = true)]
        [FieldOffset(0)]
        public int touchId;

        /// <summary>
        /// Screen-space position of the touch in pixels.
        /// </summary>
        /// <value>Screen-space position of the touch.</value>
        /// <seealso cref="TouchControl.position"/>
        [InputControl(displayName = "Position", dontReset = true)]
        [FieldOffset(4)]
        public Vector2 position;

        /// <summary>
        /// Screen-space motion delta of the touch in pixels.
        /// </summary>
        /// <value>Screen-space movement delta.</value>
        /// <seealso cref="TouchControl.delta"/>
        [InputControl(displayName = "Delta")]
        [FieldOffset(12)]
        public Vector2 delta;

        /// <summary>
        /// Pressure-level of the touch against the touchscreen.
        /// </summary>
        /// <value>Pressure of touch.</value>
        /// <remarks>
        /// The core range for this value is [0..1] with 1 indicating maximum pressure. Note, however,
        /// that the actual value may go beyond 1 in practice. This is because the system will usually
        /// define "maximum pressure" to be less than the physical maximum limit the hardware is capable
        /// of reporting so that to achieve maximum pressure, one does not need to press as hard as
        /// possible.
        /// </remarks>
        /// <seealso cref="TouchControl.pressure"/>
        [InputControl(displayName = "Pressure", layout = "Axis")]
        [FieldOffset(20)]
        public float pressure;

        /// <summary>
        /// Radius of the touch print on the surface.
        /// </summary>
        /// <value>Touch extents horizontally and vertically.</value>
        /// <remarks>
        /// The touch radius is given in screen-space pixel coordinates along X and Y centered in the middle
        /// of the touch. Note that not all screens and systems support radius detection on touches so this
        /// value may be at <c>default</c> for an otherwise perfectly valid touch.
        /// </remarks>
        /// <seealso cref="TouchControl.radius"/>
        [InputControl(displayName = "Radius")]
        [FieldOffset(24)]
        public Vector2 radius;

        /// <summary>
        /// <see cref="TouchPhase"/> value of the touch.
        /// </summary>
        /// <value>Current <see cref="TouchPhase"/>.</value>
        /// <seealso cref="phase"/>
        [InputControl(name = "phase", displayName = "Touch Phase", layout = "TouchPhase", synthetic = true)]
        [InputControl(name = "press", displayName = "Touch Contact?", layout = "TouchPress", useStateFrom = "phase")]
        [FieldOffset(32)]
        public byte phaseId;

        [InputControl(name = "tapCount", displayName = "Tap Count", layout = "Integer")]
        [FieldOffset(33)]
        public byte tapCount;

        // Not currently used, but still needed in this struct for padding,
        // as il2cpp does not implement FieldOffset.
        [FieldOffset(34)]
        byte displayIndex;

        [InputControl(name = "indirectTouch", displayName = "Indirect Touch?", layout = "Button", bit = 0, synthetic = true)]
        [InputControl(name = "tap", displayName = "Tap", layout = "Button", bit = 4)]
        [FieldOffset(35)]
        public byte flags;

        // Need four bytes of alignment here for the startTime double. Using that for storing updateStepCounts.
        // They aren't needed directly by Touchscreen but are used by EnhancedTouch and since we have the four
        // bytes, may just as well use them instead of wasting them on padding.
        [FieldOffset(36)]
        internal uint updateStepCount;

        // NOTE: The following data is NOT sent by native but rather data we add on the managed side to each touch.

        /// <summary>
        /// Time that the touch was started. Relative to <c>Time.realTimeSinceStartup</c>.
        /// </summary>
        /// <value>Time that the touch was started.</value>
        /// <remarks>
        /// This is set automatically by <see cref="Touchscreen"/> and does not need to be provided
        /// by events sent to the touchscreen.
        /// </remarks>
        /// <seealso cref="InputEvent.time"/>
        /// <seealso cref="TouchControl.startTime"/>
        [InputControl(displayName = "Start Time", layout  = "Double", synthetic = true)]
        [FieldOffset(40)]
        public double startTime; // In *external* time, i.e. currentTimeOffsetToRealtimeSinceStartup baked in.

        /// <summary>
        /// The position where the touch started.
        /// </summary>
        /// <value>Screen-space start position of the touch.</value>
        /// <remarks>
        /// This is set automatically by <see cref="Touchscreen"/> and does not need to be provided
        /// by events sent to the touchscreen.
        /// </remarks>
        /// <seealso cref="TouchControl.startPosition"/>
        [InputControl(displayName = "Start Position", synthetic = true)]
        [FieldOffset(48)]
        public Vector2 startPosition;

        /// <summary>
        /// Get or set the phase of the touch.
        /// </summary>
        /// <value>Phase of the touch.</value>
        /// <seealso cref="TouchControl.phase"/>
        public TouchPhase phase
        {
            get => (TouchPhase)phaseId;
            set => phaseId = (byte)value;
        }

        public bool isNoneEndedOrCanceled => phase == TouchPhase.None || phase == TouchPhase.Ended ||
        phase == TouchPhase.Canceled;
        public bool isInProgress => phase == TouchPhase.Began || phase == TouchPhase.Moved ||
        phase == TouchPhase.Stationary;

        /// <summary>
        /// Whether, after not having any touch contacts, this is part of the first touch contact that started.
        /// </summary>
        /// <remarks>
        /// This flag will be set internally by <see cref="Touchscreen"/>. Generally, it is
        /// not necessary to set this bit manually when feeding data to Touchscreens.
        /// </remarks>
        public bool isPrimaryTouch
        {
            get => (flags & (byte)TouchFlags.PrimaryTouch) != 0;
            set
            {
                if (value)
                    flags |= (byte)TouchFlags.PrimaryTouch;
                else
                    flags &= (byte)~TouchFlags.PrimaryTouch;
            }
        }

        internal bool isOrphanedPrimaryTouch
        {
            get => (flags & (byte)TouchFlags.OrphanedPrimaryTouch) != 0;
            set
            {
                if (value)
                    flags |= (byte)TouchFlags.OrphanedPrimaryTouch;
                else
                    flags &= (byte)~TouchFlags.OrphanedPrimaryTouch;
            }
        }

        public bool isIndirectTouch
        {
            get => (flags & (byte)TouchFlags.IndirectTouch) != 0;
            set
            {
                if (value)
                    flags |= (byte)TouchFlags.IndirectTouch;
                else
                    flags &= (byte)~TouchFlags.IndirectTouch;
            }
        }

        public bool isTap
        {
            get => isTapPress;
            set => isTapPress = value;
        }

        internal bool isTapPress
        {
            get => (flags & (byte)TouchFlags.TapPress) != 0;
            set
            {
                if (value)
                    flags |= (byte)TouchFlags.TapPress;
                else
                    flags &= (byte)~TouchFlags.TapPress;
            }
        }

        internal bool isTapRelease
        {
            get => (flags & (byte)TouchFlags.TapRelease) != 0;
            set
            {
                if (value)
                    flags |= (byte)TouchFlags.TapRelease;
                else
                    flags &= (byte)~TouchFlags.TapRelease;
            }
        }

        internal bool beganInSameFrame
        {
            get => (flags & (byte)TouchFlags.BeganInSameFrame) != 0;
            set
            {
                if (value)
                    flags |= (byte)TouchFlags.BeganInSameFrame;
                else
                    flags &= (byte)~TouchFlags.BeganInSameFrame;
            }
        }

        /// <inheritdoc/>
        public FourCC format => Format;

        /// <summary>
        /// Return a string representation of the state useful for debugging.
        /// </summary>
        /// <returns>A string representation of the touch state.</returns>
        public override string ToString()
        {
            return $"{{ id={touchId} phase={phase} pos={position} delta={delta} pressure={pressure} radius={radius} primary={isPrimaryTouch} }}";
        }
    }

    /// <summary>
    /// Default state layout for touch devices.
    /// </summary>
    /// <remarks>
    /// Combines multiple pointers each corresponding to a single contact.
    ///
    /// Normally, TODO (sending state events)
    ///
    /// All touches combine to quite a bit of state; ideally send delta events that update
    /// only specific fingers.
    ///
    /// This is NOT used by native. Instead, the native runtime always sends individual touches (<see cref="TouchState"/>)
    /// and leaves state management for a touchscreen as a whole to the managed part of the system.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = MaxTouches * TouchState.kSizeInBytes)]
    internal unsafe struct TouchscreenState : IInputStateTypeInfo
    {
        /// <summary>
        /// Memory format tag for TouchscreenState.
        /// </summary>
        /// <value>Returns "TSCR".</value>
        /// <seealso cref="InputStateBlock.format"/>
        public static FourCC Format => new FourCC('T', 'S', 'C', 'R');

        /// <summary>
        /// Maximum number of touches that can be tracked at the same time.
        /// </summary>
        /// <value>Maximum number of concurrent touches.</value>
        public const int MaxTouches = 10;

        /// <summary>
        /// Data for the touch that is deemed the "primary" touch at the moment.
        /// </summary>
        /// <remarks>
        /// This touch duplicates touch data from whichever touch is deemed the primary touch at the moment.
        /// When going from no fingers down to any finger down, the first finger to touch the screen is
        /// deemed the "primary touch". It stays the primary touch until released. At that point, if any other
        /// finger is still down, the next finger in <see cref="touchData"/> is
        ///
        /// Having this touch be its own separate state and own separate control allows actions to track the
        /// state of the primary touch even if the touch moves from one finger to another in <see cref="touchData"/>.
        /// </remarks>
        [InputControl(name = "primaryTouch", displayName = "Primary Touch", layout = "Touch", synthetic = true)]
        [InputControl(name = "primaryTouch/tap", usage = "PrimaryAction")]

        // Add controls compatible with what Pointer expects and redirect their
        // state to the state of touch0 so that this essentially becomes our
        // pointer control.
        // NOTE: Some controls from Pointer don't make sense for touch and we "park"
        //       them by assigning them invalid offsets (thus having automatic state
        //       layout put them at the end of our fixed state).
        [InputControl(name = "position", useStateFrom = "primaryTouch/position")]
        [InputControl(name = "delta", useStateFrom = "primaryTouch/delta")]
        [InputControl(name = "pressure", useStateFrom = "primaryTouch/pressure")]
        [InputControl(name = "radius", useStateFrom = "primaryTouch/radius")]
        [InputControl(name = "press", useStateFrom = "primaryTouch/phase", layout = "TouchPress", synthetic = true, usages = new string[0])]
        [FieldOffset(0)]
        public fixed byte primaryTouchData[TouchState.kSizeInBytes];

        internal const int kTouchDataOffset = TouchState.kSizeInBytes;

        [InputControl(layout = "Touch", name = "touch", displayName = "Touch", arraySize = MaxTouches)]
        [FieldOffset(kTouchDataOffset)]
        public fixed byte touchData[MaxTouches * TouchState.kSizeInBytes];

        public TouchState* primaryTouch
        {
            get
            {
                fixed(byte* ptr = primaryTouchData)
                return (TouchState*)ptr;
            }
        }

        public TouchState* touches
        {
            get
            {
                fixed(byte* ptr = touchData)
                return (TouchState*)ptr;
            }
        }

        public FourCC format => Format;
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Indicates where in its lifecycle a given touch is.
    /// </summary>
    public enum TouchPhase
    {
        ////REVIEW: Why have a separate None instead of just making this equivalent to either Ended or Canceled?
        /// <summary>
        /// No activity has been registered on the touch yet.
        /// </summary>
        /// <remarks>
        /// A given touch state will generally not go back to None once there has been input for it. Meaning that
        /// it generally indicates a default-initialized touch record.
        /// </remarks>
        None,

        /// <summary>
        /// A touch has just begun, i.e. a finger has touched the screen.. Only the first touch input in any given touch will have this phase.
        /// </summary>
        Began,

        /// <summary>
        /// An ongoing touch has changed position.
        /// </summary>
        Moved,

        /// <summary>
        /// An ongoing touch has just ended, i.e. the respective finger has been lifted off of the screen. Only the last touch input in a
        /// given touch will have this phase.
        /// </summary>
        Ended,

        /// <summary>
        /// An ongoing touch has been cancelled, i.e. ended in a way other than through user interaction. This happens, for example, if
        /// focus is moved away from the application while the touch is ongoing.
        /// </summary>
        Canceled,

        /// <summary>
        /// An ongoing touch has not been moved (not received any input) in a frame.
        /// </summary>
        /// <remarks>
        /// This phase is not used by <see cref="Touchscreen"/>. This means that <see cref="TouchControl"/> will not generally
        /// return this value for <see cref="TouchControl.phase"/>. It is, however, used by <see cref="UnityEngine.InputSystem.EnhancedTouch.Touch"/>.
        /// </remarks>
        Stationary,
    }

    /// <summary>
    /// A multi-touch surface.
    /// </summary>
    /// <remarks>
    /// Touchscreen is somewhat different from most other device implementations in that it does not usually
    /// consume input in the form of a full device snapshot but rather consumes input sent to it in the form
    /// of events containing a <see cref="TouchState"/> each. This is unusual as <see cref="TouchState"/>
    /// uses a memory format different from <see cref="TouchState.Format"/>. However, when a <c>Touchscreen</c>
    /// sees an event containing a <see cref="TouchState"/>, it will handle that event on a special code path.
    ///
    /// This allows <c>Touchscreen</c> to decide on its own which control in <see cref="touches"/> to store
    /// a touch at and to perform things such as tap detection (see <see cref="TouchControl.tap"/> and
    /// <see cref="TouchControl.tapCount"/>) and primary touch handling (see <see cref="primaryTouch"/>).
    ///
    /// <example>
    /// <code>
    /// // Create a touchscreen device.
    /// var touchscreen = InputSystem.AddDevice&lt;Touchscreen&gt;();
    ///
    /// // Send a touch to the device.
    /// InputSystem.QueueStateEvent(touchscreen,
    ///     new TouchState
    ///     {
    ///         phase = TouchPhase.Began,
    ///         // Must have a valid, non-zero touch ID. Touchscreen will not operate
    ///         // correctly if we don't set IDs properly.
    ///         touchId = 1,
    ///         position = new Vector2(123, 234),
    ///         // Delta will be computed by Touchscreen automatically.
    ///     });
    /// </code>
    /// </example>
    ///
    /// Note that this class presents a fairly low-level touch API. When working with touch from script code,
    /// it is recommended to use the higher-level <see cref="EnhancedTouch.Touch"/> API instead.
    /// </remarks>
    [InputControlLayout(stateType = typeof(TouchscreenState), isGenericTypeOfDevice = true)]
    public class Touchscreen : Pointer, IInputStateCallbackReceiver, IEventMerger
    {
        /// <summary>
        /// Synthetic control that has the data for the touch that is deemed the "primary" touch at the moment.
        /// </summary>
        /// <value>Control tracking the screen's primary touch.</value>
        /// <remarks>
        /// This touch duplicates touch data from whichever touch is deemed the primary touch at the moment.
        /// When going from no fingers down to any finger down, the first finger to touch the screen is
        /// deemed the "primary touch". It stays the primary touch until the last finger is released.
        ///
        /// Note that unlike the touch from which it originates, the primary touch will be kept ongoing for
        /// as long as there is still a finger on the screen. Put another way, <see cref="TouchControl.phase"/>
        /// of <c>primaryTouch</c> will only transition to <see cref="TouchPhase.Ended"/> once the last finger
        /// has been lifted off the screen.
        /// </remarks>
        public TouchControl primaryTouch { get; protected set; }

        /// <summary>
        /// Array of all <see cref="TouchControl"/>s on the device.
        /// </summary>
        /// <value>All <see cref="TouchControl"/>s on the screen.</value>
        /// <remarks>
        /// By default, a touchscreen will allocate 10 touch controls. This can be changed
        /// by modifying the "Touchscreen" layout itself or by derived layouts. In practice,
        /// this means that this array will usually have a fixed length of 10 entries but
        /// it may deviate from that.
        /// </remarks>
        public ReadOnlyArray<TouchControl> touches { get; protected set; }

        protected TouchControl[] touchControlArray
        {
            get => touches.m_Array;
            set => touches = new ReadOnlyArray<TouchControl>(value);
        }

        /// <summary>
        /// The touchscreen that was added or updated last or null if there is no
        /// touchscreen connected to the system.
        /// </summary>
        /// <value>Current touch screen.</value>
        public new static Touchscreen current { get; internal set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            base.FinishSetup();

            primaryTouch = GetChildControl<TouchControl>("primaryTouch");

            // Find out how many touch controls we have.
            var touchControlCount = 0;
            foreach (var child in children)
                if (child is TouchControl)
                    ++touchControlCount;

            // Keep primaryTouch out of array.
            Debug.Assert(touchControlCount >= 1, "Should have found at least primaryTouch control");
            if (touchControlCount >= 1)
                --touchControlCount;

            // Gather touch controls into array.
            var touchArray = new TouchControl[touchControlCount];
            var touchIndex = 0;
            foreach (var child in children)
            {
                if (child == primaryTouch)
                    continue;

                if (child is TouchControl control)
                    touchArray[touchIndex++] = control;
            }

            touches = new ReadOnlyArray<TouchControl>(touchArray);
        }

        // Touch has more involved state handling than most other devices. To not put touch allocation logic
        // in all the various platform backends (i.e. see a touch with a certain ID coming in from the system
        // and then having to decide *where* to store that inside of Touchscreen's state), we have backends
        // send us individual touches ('TOUC') instead of whole Touchscreen snapshots ('TSRC'). Using
        // IInputStateCallbackReceiver, Touchscreen then dynamically decides where to store the touch.
        //
        // Also, Touchscreen has bits of logic to automatically synthesize the state of controls it inherits
        // from Pointer (such as "<Pointer>/press").
        //
        // NOTE: We do *NOT* make a effort here to prevent us from losing short-lived touches. This is different
        //       from the old input system where individual touches were not reused until the next frame. This meant
        //       that additional touches potentially had to be allocated in order to accommodate new touches coming
        //       in from the system.
        //
        //       The rationale for *NOT* doing this is that:
        //
        //       a) Actions don't need it. They observe every single state change and thus will not lose data
        //          even if it is short-lived (i.e. changes more than once in the same update).
        //       b) The higher-level Touch (EnhancedTouchSupport) API is provided to
        //          not only handle this scenario but also give a generally more flexible and useful touch API
        //          than writing code directly against Touchscreen.

        protected new unsafe void OnNextUpdate()
        {
            Profiler.BeginSample("Touchscreen.OnNextUpdate");

            ////TODO: early out and skip crawling through touches if we didn't change state in the last update
            ////      (also obsoletes the need for the if() check below)
            var statePtr = currentStatePtr;
            var touchStatePtr = (TouchState*)((byte*)statePtr + stateBlock.byteOffset + TouchscreenState.kTouchDataOffset);
            for (var i = 0; i < touches.Count; ++i, ++touchStatePtr)
            {
                // Reset delta.
                if (touchStatePtr->delta != default)
                    InputState.Change(touches[i].delta, Vector2.zero);

                // Reset tap count.
                // NOTE: We are basing this on startTime rather than adding on end time of the last touch. The reason is
                //       that to do so we would have to add another record to keep track of timestamps for each touch. And
                //       since we know the maximum time that a tap can take, we have a reasonable estimate for when a prior
                //       tap must have ended.
                if (touchStatePtr->tapCount > 0 && InputState.currentTime >= touchStatePtr->startTime + s_TapTime + s_TapDelayTime)
                    InputState.Change(touches[i].tapCount, (byte)0);
            }

            var primaryTouchState = (TouchState*)((byte*)statePtr + stateBlock.byteOffset);
            if (primaryTouchState->delta != default)
                InputState.Change(primaryTouch.delta, Vector2.zero);
            if (primaryTouchState->tapCount > 0 && InputState.currentTime >= primaryTouchState->startTime + s_TapTime + s_TapDelayTime)
                InputState.Change(primaryTouch.tapCount, (byte)0);

            Profiler.EndSample();
        }

        /// <summary>
        /// Called whenever a new state event is received.
        /// </summary>
        /// <param name="eventPtr"></param>
        protected new unsafe void OnStateEvent(InputEventPtr eventPtr)
        {
            var eventType = eventPtr.type;

            // We don't allow partial updates for TouchStates.
            if (eventType == DeltaStateEvent.Type)
                return;

            // If it's not a single touch, just take the event state as is (will have to be TouchscreenState).
            var stateEventPtr = StateEvent.FromUnchecked(eventPtr);
            if (stateEventPtr->stateFormat != TouchState.Format)
            {
                InputState.Change(this, eventPtr);
                return;
            }

            Profiler.BeginSample("TouchAllocate");

            // For performance reasons, we read memory here directly rather than going through
            // ReadValue() of the individual TouchControl children. This means that Touchscreen,
            // unlike other devices, is hardwired to a single memory layout only.

            var statePtr = currentStatePtr;
            var currentTouchState = (TouchState*)((byte*)statePtr + touches[0].stateBlock.byteOffset);
            var primaryTouchState = (TouchState*)((byte*)statePtr + primaryTouch.stateBlock.byteOffset);
            var touchControlCount = touches.Count;

            // Native does not send a full TouchState as we define it here. We have added some fields
            // that we store internally. Make sure we don't read invalid memory here and copy only what
            // we got.
            TouchState newTouchState;
            if (stateEventPtr->stateSizeInBytes == TouchState.kSizeInBytes)
            {
                newTouchState = *(TouchState*)stateEventPtr->state;
            }
            else
            {
                newTouchState = default;
                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref newTouchState), stateEventPtr->state, stateEventPtr->stateSizeInBytes);
            }

            // Make sure we're not getting thrown off by noise on fields that we don't want to
            // pick up from input.
            newTouchState.tapCount = 0;
            newTouchState.isTapPress = false;
            newTouchState.isTapRelease = false;
            newTouchState.updateStepCount = InputUpdate.s_UpdateStepCount;

            ////REVIEW: The logic in here makes us inherently susceptible to the ordering of the touch events in the event
            ////        stream. I believe we have platforms (Android?) that send us touch events finger-by-finger (or touch-by-touch?)
            ////        rather than sorted by time. This will probably screw up the logic in here.

            // If it's an ongoing touch, try to find the TouchState we have allocated to the touch
            // previously.
            var phase = newTouchState.phase;
            if (phase != TouchPhase.Began)
            {
                var touchId = newTouchState.touchId;
                for (var i = 0; i < touchControlCount; ++i)
                {
                    if (currentTouchState[i].touchId == touchId)
                    {
                        // Preserve primary touch state.
                        var isPrimaryTouch = currentTouchState[i].isPrimaryTouch;
                        newTouchState.isPrimaryTouch = isPrimaryTouch;

                        // Compute delta if touch doesn't have one.
                        if (newTouchState.delta == default)
                            newTouchState.delta = newTouchState.position - currentTouchState[i].position;

                        // Accumulate delta.
                        newTouchState.delta += currentTouchState[i].delta;

                        // Keep start time and position.
                        newTouchState.startTime = currentTouchState[i].startTime;
                        newTouchState.startPosition = currentTouchState[i].startPosition;

                        // Detect taps.
                        var isTap = newTouchState.isNoneEndedOrCanceled &&
                            (eventPtr.time - newTouchState.startTime) <= s_TapTime &&
                            ////REVIEW: this only takes the final delta to start position into account, not the delta over the lifetime of the
                            ////        touch; is this robust enough or do we need to make sure that we never move more than the tap radius
                            ////        over the entire lifetime of the touch?
                            (newTouchState.position - newTouchState.startPosition).sqrMagnitude <= s_TapRadiusSquared;
                        if (isTap)
                            newTouchState.tapCount = (byte)(currentTouchState[i].tapCount + 1);
                        else
                            newTouchState.tapCount = currentTouchState[i].tapCount; // Preserve tap count; reset in OnCarryStateForward.

                        // Update primary touch.
                        if (isPrimaryTouch)
                        {
                            if (newTouchState.isNoneEndedOrCanceled)
                            {
                                ////REVIEW: also reset tapCounts here when tap delay time has expired on the touch?

                                newTouchState.isPrimaryTouch = false;

                                // Primary touch was ended. See if there are still other ongoing touches.
                                var haveOngoingTouch = false;
                                for (var n = 0; n < touchControlCount; ++n)
                                {
                                    if (n == i)
                                        continue;

                                    if (currentTouchState[n].isInProgress)
                                    {
                                        haveOngoingTouch = true;
                                        break;
                                    }
                                }

                                if (!haveOngoingTouch)
                                {
                                    // No, primary was the only ongoing touch. End it.

                                    if (isTap)
                                        TriggerTap(primaryTouch, ref newTouchState, eventPtr);
                                    else
                                        InputState.Change(primaryTouch, ref newTouchState, eventPtr: eventPtr);
                                }
                                else
                                {
                                    // Yes, we have other touches going on. Make the primary touch an
                                    // orphan and wait until the other touches are released.

                                    var newPrimaryTouchState = newTouchState;
                                    newPrimaryTouchState.phase = TouchPhase.Moved;
                                    newPrimaryTouchState.isOrphanedPrimaryTouch = true;
                                    InputState.Change(primaryTouch, ref newPrimaryTouchState, eventPtr: eventPtr);
                                }
                            }
                            else
                            {
                                // Primary touch was updated.
                                InputState.Change(primaryTouch, ref newTouchState, eventPtr: eventPtr);
                            }
                        }
                        else
                        {
                            // If it's not the primary touch but the touch has ended, see if we have an
                            // orphaned primary touch. If so, end it now.
                            if (newTouchState.isNoneEndedOrCanceled && primaryTouchState->isOrphanedPrimaryTouch)
                            {
                                var haveOngoingTouch = false;
                                for (var n = 0; n < touchControlCount; ++n)
                                {
                                    if (n == i)
                                        continue;

                                    if (currentTouchState[n].isInProgress)
                                    {
                                        haveOngoingTouch = true;
                                        break;
                                    }
                                }

                                if (!haveOngoingTouch)
                                {
                                    primaryTouchState->isOrphanedPrimaryTouch = false;
                                    InputState.Change(primaryTouch.phase, (byte)TouchPhase.Ended);
                                }
                            }
                        }

                        if (isTap)
                        {
                            // Make tap button go down and up.
                            //
                            // NOTE: We do this here instead of right away up there when we detect the touch so
                            //       that the state change notifications go together. First those for the primary
                            //       touch, then the ones for the touch record itself.
                            TriggerTap(touches[i], ref newTouchState, eventPtr);
                        }
                        else
                        {
                            InputState.Change(touches[i], ref newTouchState, eventPtr: eventPtr);
                        }

                        Profiler.EndSample();
                        return;
                    }
                }

                // Couldn't find an entry. Either it was a touch that we previously ran out of available
                // entries for or it's an event sent out of sequence. Ignore the touch to be consistent.

                Profiler.EndSample();
                return;
            }

            // It's a new touch. Try to find an unused TouchState.
            for (var i = 0; i < touchControlCount; ++i, ++currentTouchState)
            {
                // NOTE: We're overwriting any ended touch immediately here. This means we immediately overwrite even
                //       if we still have other unused slots. What this gives us is a completely predictable touch #0..#N
                //       sequence (i.e. touch #N is only ever used if there are indeed #N concurrently touches). However,
                //       it does mean that we overwrite state aggressively. If you are not using actions or the higher-level
                //       Touch API, be aware of this!
                if (currentTouchState->isNoneEndedOrCanceled)
                {
                    newTouchState.delta = Vector2.zero;
                    newTouchState.startTime = eventPtr.time;
                    newTouchState.startPosition = newTouchState.position;

                    // Make sure we're not picking up noise sent from native.
                    newTouchState.isPrimaryTouch = false;
                    newTouchState.isOrphanedPrimaryTouch = false;
                    newTouchState.isTap = false;

                    // Tap counts are preserved from prior touches on the same finger.
                    newTouchState.tapCount = currentTouchState->tapCount;

                    // Make primary touch, if there's none currently.
                    if (primaryTouchState->isNoneEndedOrCanceled)
                    {
                        newTouchState.isPrimaryTouch = true;
                        InputState.Change(primaryTouch, ref newTouchState, eventPtr: eventPtr);
                    }

                    InputState.Change(touches[i], ref newTouchState, eventPtr: eventPtr);

                    Profiler.EndSample();
                    return;
                }
            }

            // We ran out of state and we don't want to stomp an existing ongoing touch.
            // Drop this touch entirely.
            // NOTE: Getting here means we're having fewer touch entries than the number of concurrent touches supported
            //       by the backend (or someone is simply sending us nonsense data).

            Profiler.EndSample();
        }

        void IInputStateCallbackReceiver.OnNextUpdate()
        {
            OnNextUpdate();
        }

        void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
        {
            OnStateEvent(eventPtr);
        }

        unsafe bool IInputStateCallbackReceiver.GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            // This code goes back to the trickery we perform in OnStateEvent. We consume events in TouchState format
            // instead of in TouchscreenState format. This means that the input system does not know how the state in those
            // events correlates to the controls we have.
            //
            // This method is used to give the input system an offset based on which the input system can compute relative
            // offsets into the state of eventPtr for controls that are part of the control hierarchy rooted at 'control'.

            if (!eventPtr.IsA<StateEvent>())
                return false;

            var stateEventPtr = StateEvent.FromUnchecked(eventPtr);
            if (stateEventPtr->stateFormat != TouchState.Format)
                return false;

            // If we get a null control and a TouchState event, all the system wants to know is what
            // state offset to use to make sense of the event.
            if (control == null)
            {
                // We can't say which specific touch this would go to (if any at all) without going through
                // the same logic that we run through in OnStateEvent. For the sake of just being able to read
                // out data from a touch event, it'd be enough to return the offset of *any* TouchControl here.
                // But for the sake of being able to compare the data in an event to that in the Touchscreen,
                // this would not be enough. Thus we make an attempt here at locating a touch record which *should*
                // be receiving the event if it were to be processed by OnStateEvent.

                var currentTouchState = (TouchState*)((byte*)currentStatePtr + touches[0].stateBlock.byteOffset);
                var eventTouchState = (TouchState*)stateEventPtr->state;
                var eventTouchId = eventTouchState->touchId;
                var eventTouchPhase = eventTouchState->phase;

                var touchControlCount = touches.Count;
                for (var i = 0; i < touchControlCount; ++i)
                {
                    var touch = &currentTouchState[i];
                    if (touch->touchId == eventTouchId || (!touch->isInProgress && eventTouchPhase.IsActive()))
                    {
                        offset = primaryTouch.m_StateBlock.byteOffset + primaryTouch.m_StateBlock.alignedSizeInBytes - m_StateBlock.byteOffset +
                            (uint)(i * UnsafeUtility.SizeOf<TouchState>());
                        return true;
                    }
                }

                return false;
            }

            // The only controls we can read out from a TouchState event are those that are part of TouchControl
            // (and part of this Touchscreen).
            var touchControl = control.FindInParentChain<TouchControl>();
            if (touchControl == null || touchControl.parent != this)
                return false;

            // We could allow *any* of the TouchControls on the Touchscreen here. We'd simply base the
            // offset on the TouchControl of the 'control' we get as an argument.
            //
            // However, doing that would mean that all the TouchControls would map into the same input event.
            // So when a piece of code like in InputUser goes and cycles through all controls to determine ones
            // that have changed in an event, it would find that instead of a single touch position value changing,
            // all of them would be changing from the same single event.
            //
            // For this reason, we lock things down to the primaryTouch control.

            if (touchControl != primaryTouch)
                return false;

            offset = touchControl.stateBlock.byteOffset - m_StateBlock.byteOffset;
            return true;
        }

        internal static unsafe bool MergeForward(InputEventPtr currentEventPtr, InputEventPtr nextEventPtr)
        {
            if (currentEventPtr.type != StateEvent.Type || nextEventPtr.type != StateEvent.Type)
                return false;

            var currentEvent = StateEvent.FromUnchecked(currentEventPtr);
            var nextEvent = StateEvent.FromUnchecked(nextEventPtr);

            if (currentEvent->stateFormat != TouchState.Format || nextEvent->stateFormat != TouchState.Format)
                return false;

            var currentState = (TouchState*)currentEvent->state;
            var nextState = (TouchState*)nextEvent->state;

            if (currentState->touchId != nextState->touchId || currentState->phaseId != nextState->phaseId || currentState->flags != nextState->flags)
                return false;

            nextState->delta += currentState->delta;

            return true;
        }

        bool IEventMerger.MergeForward(InputEventPtr currentEventPtr, InputEventPtr nextEventPtr)
        {
            return MergeForward(currentEventPtr, nextEventPtr);
        }

        // We can only detect taps on touch *release*. At which point it acts like a button that triggers and releases
        // in one operation.
        private static void TriggerTap(TouchControl control, ref TouchState state, InputEventPtr eventPtr)
        {
            ////REVIEW: we're updating the entire TouchControl here; we could update just the tap state using a delta event; problem
            ////        is that the tap *down* still needs a full update on the state

            // We don't increase tapCount here as we may be sending the tap from the same state to both the TouchControl
            // that got tapped and to primaryTouch.

            // Press.
            state.isTapPress = true;
            state.isTapRelease = false;
            InputState.Change(control, ref state, eventPtr: eventPtr);

            // Release.
            state.isTapPress = false;
            state.isTapRelease = true;
            InputState.Change(control, ref state, eventPtr: eventPtr);
            state.isTapRelease = false;
        }

        internal static float s_TapTime;
        internal static float s_TapDelayTime;
        internal static float s_TapRadiusSquared;
    }
}
