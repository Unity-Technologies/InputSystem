using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

////TODO: recorded times are baked *external* times; reset touch when coming out of play mode

////REVIEW: record velocity on touches? or add method to very easily get the data?

namespace UnityEngine.InputSystem.EnhancedTouch
{
    /// <summary>
    /// A high-level representation of a touch which automatically keeps track of a touch
    /// over time.
    /// </summary>
    /// <remarks>
    /// This API obsoletes the need for manually keeping tracking of touch IDs (<see cref="TouchControl.touchId"/>)
    /// and touch phases (<see cref="TouchControl.phase"/>) in order to tell one touch apart from another.
    ///
    /// Also, this class protects against losing touches. If a touch is shorter-lived than a single input update,
    /// <see cref="Touchscreen"/> may overwrite it with a new touch coming in in the same update whereas this class
    /// will retain all changes that happened on the touchscreen in any particular update.
    ///
    /// The API makes a distinction between "fingers" and "touches". A touch refers to one contact state change event, i.e. a
    /// finger beginning to touch the screen (<see cref="TouchPhase.Began"/>), moving on the screen (<see cref="TouchPhase.Moved"/>),
    /// or being lifted off the screen (<see cref="TouchPhase.Ended"/> or <see cref="TouchPhase.Canceled"/>).
    /// A finger, on the other hand, always refers to the Nth contact on the screen.
    ///
    /// A Touch instance is a struct which only contains a reference to the actual data which is stored in unmanaged
    /// memory.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public struct Touch : IEquatable<Touch>
    {
        // The way this works is that at the core, it simply attaches one InputStateHistory per "<Touchscreen>/touch*"
        // control and then presents a public API that crawls over the recorded touch history in various ways.

        /// <summary>
        /// Whether this touch record holds valid data.
        /// </summary>
        /// <value>If true, the data contained in the touch is valid.</value>
        /// <remarks>
        /// Touch data is stored in unmanaged memory as a circular input buffer. This means that when
        /// the buffer runs out of capacity, older touch entries will get reused. When this happens,
        /// existing <c>Touch</c> instances referring to the record become invalid.
        ///
        /// This property can be used to determine whether the record held on to by the <c>Touch</c>
        /// instance is still valid.
        ///
        /// This property will be <c>false</c> for default-initialized <c>Touch</c> instances.
        ///
        /// Note that accessing most of the other properties on this struct when the touch is
        /// invalid will trigger <c>InvalidOperationException</c>.
        /// </remarks>
        public bool valid => m_TouchRecord.valid;

        /// <summary>
        /// The finger used for the touch contact. Null only for default-initialized
        /// instances of the struct.
        /// </summary>
        /// <value>Finger used for the touch contact.</value>
        /// <seealso cref="activeFingers"/>
        public Finger finger => m_Finger;

        /// <summary>
        /// Current phase of the touch.
        /// </summary>
        /// <value>Current phase of the touch.</value>
        /// <remarks>
        /// Every touch goes through a predefined cycle that starts with <see cref="TouchPhase.Began"/>,
        /// then potentially <see cref="TouchPhase.Moved"/> and/or <see cref="TouchPhase.Stationary"/>,
        /// and finally concludes with either <see cref="TouchPhase.Ended"/> or <see cref="TouchPhase.Canceled"/>.
        ///
        /// This property indicates where in the cycle the touch is.
        /// </remarks>
        /// <seealso cref="isInProgress"/>
        /// <seealso cref="TouchControl.phase"/>
        public TouchPhase phase => state.phase;

        /// <summary>
        /// Unique ID of the touch as (usually) assigned by the platform.
        /// </summary>
        /// <value>Unique, non-zero ID of the touch.</value>
        /// <remarks>
        /// Each touch contact that is made with the screen receives its own unique ID which is
        /// normally assigned by the underlying platform.
        ///
        /// Note a platform may reuse touch IDs after their respective touches have finished.
        /// This means that the guarantee of uniqueness is only made with respect to <see cref="activeTouches"/>.
        ///
        /// In particular, all touches in <see cref="history"/> will have the same ID whereas
        /// touches in the a finger's <see cref="Finger.touchHistory"/> may end up having the same
        /// touch ID even though constituting different physical touch contacts.
        /// </remarks>
        /// <seealso cref="TouchControl.touchId"/>
        public int touchId => state.touchId;

        /// <summary>
        /// Normalized pressure of the touch against the touch surface.
        /// </summary>
        /// <value>Pressure level of the touch.</value>
        /// <remarks>
        /// Not all touchscreens are pressure-sensitive. If unsupported, this property will
        /// always return 0.
        ///
        /// In general, touch pressure is supported on mobile platforms only.
        ///
        /// Note that it is possible for the value to go above 1 even though it is considered normalized. The reason is
        /// that calibration on the system can put the maximum pressure point below the physically supported maximum value.
        /// </remarks>
        /// <seealso cref="TouchControl.pressure"/>
        public float pressure => state.pressure;

        /// <summary>
        /// Screen-space radius of the touch.
        /// </summary>
        /// <value>Horizontal and vertical extents of the touch contact.</value>
        /// <remarks>
        /// If supported by the underlying device, this reports the size of the touch contact based on its
        /// <see cref="screenPosition"/> center point. If not supported, this will be <c>default(Vector2)</c>.
        /// </remarks>
        /// <seealso cref="TouchControl.radius"/>
        public Vector2 radius => state.radius;

        /// <summary>
        /// Time in seconds on the same timeline as <c>Time.realTimeSinceStartup</c> when the touch began.
        /// </summary>
        /// <value>Start time of the touch.</value>
        /// <remarks>
        /// This is the value of <see cref="InputEvent.time"/> when the touch started with
        /// <see cref="phase"/> <see cref="TouchPhase.Began"/>.
        /// </remarks>
        /// <seealso cref="TouchControl.startTime"/>
        public double startTime => state.startTime;

        /// <summary>
        /// Time in seconds on the same timeline as <c>Time.realTimeSinceStartup</c> when the touch record was
        /// reported.
        /// </summary>
        /// <value>Time the touch record was reported.</value>
        /// <remarks>
        /// This is the value <see cref="InputEvent.time"/> of the event that signaled the current state
        /// change for the touch.
        /// </remarks>
        public double time => m_TouchRecord.time;

        /// <summary>
        /// The touchscreen on which the touch occurred.
        /// </summary>
        /// <value>Touchscreen associated with the touch.</value>
        public Touchscreen screen => finger.screen;

        /// <summary>
        /// Screen-space position of the touch.
        /// </summary>
        /// <value>Screen-space position of the touch.</value>
        /// <seealso cref="TouchControl.position"/>
        public Vector2 screenPosition => state.position;

        /// <summary>
        /// Screen-space position where the touch started.
        /// </summary>
        /// <value>Start position of the touch.</value>
        /// <seealso cref="TouchControl.startPosition"/>
        public Vector2 startScreenPosition => state.startPosition;

        /// <summary>
        /// Screen-space motion delta of the touch.
        /// </summary>
        /// <value>Screen-space motion delta of the touch.</value>
        /// <remarks>
        /// Note that deltas have behaviors attached to them different from most other
        /// controls. See <see cref="Pointer.delta"/> for details.
        /// </remarks>
        /// <seealso cref="TouchControl.delta"/>
        public Vector2 delta => state.delta;

        /// <summary>
        /// Number of times that the touch has been tapped in succession.
        /// </summary>
        /// <value>Indicates how many taps have been performed one after the other.</value>
        /// <remarks>
        /// Successive taps have to come within <see cref="InputSettings.multiTapDelayTime"/> for them
        /// to increase the tap count. I.e. if a new tap finishes within that time after <see cref="startTime"/>
        /// of the previous touch, the tap count is increased by one. If more than <see cref="InputSettings.multiTapDelayTime"/>
        /// passes after a tap with no successive tap, the tap count is reset to zero.
        /// </remarks>
        /// <seealso cref="TouchControl.tapCount"/>
        public int tapCount => state.tapCount;

        /// <summary>
        /// Whether the touch has performed a tap.
        /// </summary>
        /// <value>Indicates whether the touch has tapped the screen.</value>
        /// <remarks>
        /// A tap is defined as a touch that begins and ends within <see cref="InputSettings.defaultTapTime"/> and
        /// stays within <see cref="InputSettings.tapRadius"/> of its <see cref="startScreenPosition"/>. If this
        /// is the case for a touch, this button is set to 1 at the time the touch goes to <see cref="phase"/>
        /// <see cref="TouchPhase.Ended"/>.
        ///
        /// Resets to 0 only when another touch is started on the control or when the control is reset.
        /// </remarks>
        /// <seealso cref="tapCount"/>
        /// <seealso cref="InputSettings.defaultTapTime"/>
        /// <seealso cref="TouchControl.tap"/>
        public bool isTap => state.isTap;

        /// <summary>
        /// Whether the touch is currently in progress, i.e. has a <see cref="phase"/> of
        /// <see cref="TouchPhase.Began"/>, <see cref="TouchPhase.Moved"/>, or <see cref="TouchPhase.Stationary"/>.
        /// </summary>
        /// <value>Whether the touch is currently ongoing.</value>
        public bool isInProgress
        {
            get
            {
                switch (phase)
                {
                    case TouchPhase.Began:
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        return true;
                }
                return false;
            }
        }

        internal uint updateStepCount => extraData.updateStepCount;
        internal uint uniqueId => extraData.uniqueId;

        private unsafe ref TouchState state => ref *(TouchState*)m_TouchRecord.GetUnsafeMemoryPtr();
        private unsafe ref ExtraDataPerTouchState extraData =>
            ref *(ExtraDataPerTouchState*)m_TouchRecord.GetUnsafeExtraMemoryPtr();

        /// <summary>
        /// History for this specific touch.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Finger.touchHistory"/>, this gives the history of this touch only.
        /// </remarks>
        public TouchHistory history
        {
            get
            {
                if (!valid)
                    throw new InvalidOperationException("Touch is invalid");
                return finger.GetTouchHistory(this);
            }
        }

        /// <summary>
        /// All touches that are either on-going as of the current frame or have ended in the current frame.
        /// </summary>
        /// <remarks>
        /// Note that the fact that ended touches are being kept around until the end of a frame means that this
        /// array may have more touches than the total number of concurrent touches supported by the system.
        /// </remarks>
        public static ReadOnlyArray<Touch> activeTouches
        {
            get
            {
                // We lazily construct the array of active touches.
                s_PlayerState.UpdateActiveTouches();
                return new ReadOnlyArray<Touch>(s_PlayerState.activeTouches, 0, s_PlayerState.activeTouchCount);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// The length of this array will always correspond to the maximum number of concurrent touches supported by the system.
        /// </remarks>
        public static ReadOnlyArray<Finger> fingers =>
            new ReadOnlyArray<Finger>(s_PlayerState.fingers, 0, s_PlayerState.totalFingerCount);

        public static ReadOnlyArray<Finger> activeFingers
        {
            get
            {
                // We lazily construct the array of active fingers.
                s_PlayerState.UpdateActiveFingers();
                return new ReadOnlyArray<Finger>(s_PlayerState.activeFingers, 0, s_PlayerState.activeFingerCount);
            }
        }

        public static IEnumerable<Touchscreen> screens => s_Touchscreens;

        public static event Action<Finger> onFingerDown
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_OnFingerDown.AppendWithCapacity(value);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var index = s_OnFingerDown.IndexOf(value);
                if (index != -1)
                    s_OnFingerDown.RemoveAtWithCapacity(index);
            }
        }

        public static event Action<Finger> onFingerUp
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_OnFingerUp.AppendWithCapacity(value);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var index = s_OnFingerUp.IndexOf(value);
                if (index != -1)
                    s_OnFingerUp.RemoveAtWithCapacity(index);
            }
        }

        public static event Action<Finger> onFingerMove
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_OnFingerMove.AppendWithCapacity(value);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var index = s_OnFingerMove.IndexOf(value);
                if (index != -1)
                    s_OnFingerMove.RemoveAtWithCapacity(index);
            }
        }

        /*
        public static Action<Finger> onFingerTap
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        */

        /// <summary>
        /// The amount of history kept for each single touch.
        /// </summary>
        /// <remarks>
        /// By default, this is zero meaning that no history information is kept for
        /// touches. Setting this to <c>Int32.maxValue</c> will cause all history from
        /// the beginning to the end of a touch being kept.
        /// </remarks>
        public static int maxHistoryLengthPerFinger
        {
            get => s_HistoryLengthPerFinger;

            ////TODO
            /*set { throw new NotImplementedException(); }*/
        }

        internal Touch(Finger finger, InputStateHistory<TouchState>.Record touchRecord)
        {
            m_Finger = finger;
            m_TouchRecord = touchRecord;
        }

        public override string ToString()
        {
            if (!valid)
                return "<None>";

            return $"{{finger={finger.index} touchId={touchId} phase={phase} position={screenPosition} time={time}}}";
        }

        public bool Equals(Touch other)
        {
            return Equals(m_Finger, other.m_Finger) && m_TouchRecord.Equals(other.m_TouchRecord);
        }

        public override bool Equals(object obj)
        {
            return obj is Touch other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((m_Finger != null ? m_Finger.GetHashCode() : 0) * 397) ^ m_TouchRecord.GetHashCode();
            }
        }

        internal static void AddTouchscreen(Touchscreen screen)
        {
            Debug.Assert(!s_Touchscreens.ContainsReference(screen), "Already added touchscreen");
            s_Touchscreens.AppendWithCapacity(screen, capacityIncrement: 5);

            // Add finger tracking to states.
            s_PlayerState.AddFingers(screen);
            #if UNITY_EDITOR
            s_EditorState.AddFingers(screen);
            #endif
        }

        internal static void RemoveTouchscreen(Touchscreen screen)
        {
            Debug.Assert(s_Touchscreens.ContainsReference(screen), "Did not add touchscreen");

            // Remove from list.
            var index = s_Touchscreens.IndexOfReference(screen);
            s_Touchscreens.RemoveAtWithCapacity(index);

            // Remove fingers from states.
            s_PlayerState.RemoveFingers(screen);
            #if UNITY_EDITOR
            s_EditorState.RemoveFingers(screen);
            #endif
        }

        //only have this hooked when we actually need it
        internal static void BeginUpdate()
        {
            #if UNITY_EDITOR
            if ((InputState.currentUpdateType == InputUpdateType.Editor && s_PlayerState.updateMask != InputUpdateType.Editor) ||
                (InputState.currentUpdateType != InputUpdateType.Editor && s_PlayerState.updateMask == InputUpdateType.Editor))
            {
                // Either swap in editor state and retain currently active player state in s_EditorState
                // or swap player state back in.
                MemoryHelpers.Swap(ref s_PlayerState, ref s_EditorState);
            }
            #endif

            ++s_PlayerState.updateStepCount;
            s_PlayerState.haveBuiltActiveTouches = false;
        }

        private readonly Finger m_Finger;
        internal InputStateHistory<TouchState>.Record m_TouchRecord;

        internal static InlinedArray<Touchscreen> s_Touchscreens;
        internal static int s_HistoryLengthPerFinger = 64;
        internal static InlinedArray<Action<Finger>> s_OnFingerDown;
        internal static InlinedArray<Action<Finger>> s_OnFingerMove;
        internal static InlinedArray<Action<Finger>> s_OnFingerUp;

        internal static FingerAndTouchState s_PlayerState;
        #if UNITY_EDITOR
        internal static FingerAndTouchState s_EditorState;
        #endif

        // In scenarios where we have to support multiple different types of input updates (e.g. in editor or in
        // player when both dynamic and fixed input updates are enabled), we need more than one copy of touch state.
        // We encapsulate the state in this struct so that we can easily swap it.
        //
        // NOTE: Finger instances are per state. This means that you will actually see different Finger instances for
        //       the same finger in two different update types.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
            Justification = "Managed internally")]
        internal struct FingerAndTouchState
        {
            public InputUpdateType updateMask;
            public uint updateStepCount;
            public Finger[] fingers;
            public Finger[] activeFingers;
            public Touch[] activeTouches;
            public int activeFingerCount;
            public int activeTouchCount;
            public int totalFingerCount;
            public uint lastId;
            public bool haveBuiltActiveTouches;

            // `activeTouches` adds yet another view of input state that is different from "normal" recorded
            // state history. In this view, touches become stationary in the next update and deltas reset
            // between updates. We solve this by storing state separately for active touches. We *only* do
            // so when `activeTouches` is actually queried meaning that `activeTouches` has no overhead if
            // not used.
            public InputStateHistory<TouchState> activeTouchState;

            public void AddFingers(Touchscreen screen)
            {
                var touchCount = screen.touches.Count;
                ArrayHelpers.EnsureCapacity(ref fingers, totalFingerCount, touchCount);
                for (var i = 0; i < touchCount; ++i)
                {
                    var finger = new Finger(screen, i, updateMask);
                    ArrayHelpers.AppendWithCapacity(ref fingers, ref totalFingerCount, finger);
                }
            }

            public void RemoveFingers(Touchscreen screen)
            {
                var touchCount = screen.touches.Count;
                for (var i = 0; i < fingers.Length; ++i)
                {
                    if (fingers[i].screen != screen)
                        continue;

                    // Release unmanaged memory.
                    for (var n = 0; n < touchCount; ++n)
                        fingers[i + n].m_StateHistory.Dispose();

                    ////REVIEW: leave Fingers in place and reuse the instances?
                    ArrayHelpers.EraseSliceWithCapacity(ref fingers, ref totalFingerCount, i, touchCount);
                    break;
                }

                // Force rebuilding of active touches.
                haveBuiltActiveTouches = false;
            }

            public void Destroy()
            {
                for (var i = 0; i < totalFingerCount; ++i)
                    fingers[i].m_StateHistory.Dispose();
                activeTouchState?.Dispose();
                activeTouchState = null;
            }

            public void UpdateActiveFingers()
            {
                ////TODO: do this only once per update per activeFingers getter

                activeFingerCount = 0;
                for (var i = 0; i < totalFingerCount; ++i)
                {
                    var finger = fingers[i];
                    var lastTouch = finger.currentTouch;
                    if (lastTouch.valid)
                        ArrayHelpers.AppendWithCapacity(ref activeFingers, ref activeFingerCount, finger);
                }
            }

            public unsafe void UpdateActiveTouches()
            {
                if (haveBuiltActiveTouches)
                    return;

                // Clear activeTouches state.
                if (activeTouchState == null)
                {
                    activeTouchState = new InputStateHistory<TouchState>();
                    activeTouchState.extraMemoryPerRecord = UnsafeUtility.SizeOf<ExtraDataPerTouchState>();
                }
                else
                    activeTouchState.Clear();
                activeTouchCount = 0;

                // Go through fingers and for each one, get the touches that were active this update.
                for (var i = 0; i < totalFingerCount; ++i)
                {
                    var finger = fingers[i];

                    // Skip going through the finger's touches if the finger does not have an active touch.
                    // Avoids doing unnecessary work of sifting through the finger's history.
                    if (!finger.currentTouch.valid)
                        continue;

                    // We're walking newest-first through the touch history but want the resulting list of
                    // active touches to be oldest first (so that a record for an ended touch comes before
                    // a record of a new touch started on the same finger). To achieve that, we insert
                    // new touch entries for any finger always at the same index (i.e. we prepend rather
                    // than append).
                    var insertAt = activeTouchCount;

                    // Go back in time through the touch records on the finger and collect any touch
                    // active in the current frame. Note that this may yield *multiple* touches for the
                    // finger as there may be touched that have ended in the frame while in the same
                    // frame, a new touch was started.
                    var history = finger.m_StateHistory;
                    var touchRecordCount = history.Count;
                    var currentTouchId = 0;
                    for (var n = touchRecordCount - 1; n >= 0; --n)
                    {
                        var record = history[n];
                        var state = *(TouchState*)record.GetUnsafeMemoryPtr();
                        var extra = (ExtraDataPerTouchState*)record.GetUnsafeExtraMemoryPtr();

                        // Skip if part of an ongoing touch we've already recorded.
                        if (state.touchId == currentTouchId && !state.phase.IsEndedOrCanceled())
                            continue;

                        // If the touch is older than the current frame and it's a touch that has
                        // ended, we don't need to look further back into the history as anything
                        // coming before that will be equally outdated.
                        var wasUpdatedThisFrame = extra->updateStepCount == updateStepCount;
                        if (!wasUpdatedThisFrame && state.phase.IsEndedOrCanceled())
                            break;

                        // Make a copy of the touch so that we can modify data like deltas and phase.
                        var newRecord = activeTouchState.AddRecord(record);
                        var newTouch = new Touch(finger, newRecord);

                        // If the touch hasn't moved this frame, mark it stationary.
                        if ((state.phase == TouchPhase.Moved || state.phase == TouchPhase.Began) &&
                            !wasUpdatedThisFrame)
                            ((TouchState*)newRecord.GetUnsafeMemoryPtr())->phase = TouchPhase.Stationary;

                        // If the touch is hasn't moved or ended this frame, zero out its delta.
                        if (!((state.phase == TouchPhase.Moved || state.phase == TouchPhase.Ended) &&
                              wasUpdatedThisFrame))
                        {
                            ((TouchState*)newRecord.GetUnsafeMemoryPtr())->delta = new Vector2();
                        }
                        else
                        {
                            // We want accumulated deltas only on activeTouches.
                            ((TouchState*)newRecord.GetUnsafeMemoryPtr())->delta =
                                ((ExtraDataPerTouchState*)newRecord.GetUnsafeExtraMemoryPtr())->accumulatedDelta;
                        }

                        ArrayHelpers.InsertAtWithCapacity(ref activeTouches, ref activeTouchCount, insertAt, newTouch);
                        currentTouchId = state.touchId;
                    }
                }

                haveBuiltActiveTouches = true;
            }
        }

        internal struct ExtraDataPerTouchState
        {
            public Vector2 accumulatedDelta;
            public uint updateStepCount;

            // We can't guarantee that the platform is not reusing touch IDs.
            public uint uniqueId;

            ////TODO
            //public uint tapCount;
        }
    }
}
