using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

////TODO: recorded times are baked *external* times; reset touch when coming out of play mode

////REVIEW: record velocity on touches? or add method to very easily get the data?

////REVIEW: do we need to keep old touches around on activeTouches like the old UnityEngine touch API?

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
    /// The API makes a distinction between "fingers" and "touches". A touch refers to one contact state change event, that is, a
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
        /// Whether the touch has begun this frame, i.e. whether <see cref="phase"/> is <see cref="TouchPhase.Began"/>.
        /// </summary>
        /// <seealso cref="phase"/>
        /// <seealso cref="ended"/>
        /// <seealso cref="inProgress"/>
        public bool began => phase == TouchPhase.Began;

        /// <summary>
        /// Whether the touch is currently in progress, i.e. whether <see cref="phase"/> is either
        /// <see cref="TouchPhase.Moved"/>, <see cref="TouchPhase.Stationary"/>, or <see cref="TouchPhase.Began"/>.
        /// </summary>
        /// <seealso cref="phase"/>
        /// <seealso cref="began"/>
        /// <seealso cref="ended"/>
        public bool inProgress => phase == TouchPhase.Moved || phase == TouchPhase.Stationary || phase == TouchPhase.Began;

        /// <summary>
        /// Whether the touch has ended this frame, i.e. whether <see cref="phase"/> is either
        /// <see cref="TouchPhase.Ended"/> or <see cref="TouchPhase.Canceled"/>.
        /// </summary>
        /// <seealso cref="phase"/>
        /// <seealso cref="began"/>
        /// <seealso cref="isInProgress"/>
        public bool ended => phase == TouchPhase.Ended || phase == TouchPhase.Canceled;

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

        internal uint updateStepCount => state.updateStepCount;
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
        /// A touch that begins in a frame will always have its phase set to <see cref="TouchPhase.Began"/> even
        /// if there was also movement (or even an end/cancellation) for the touch in the same frame.
        ///
        /// A touch that begins and ends in the same frame will have its <see cref="TouchPhase.Began"/> surface
        /// in that frame and then another entry with <see cref="TouchPhase.Ended"/> surface in the
        /// <em>next</em> frame. This logic implies that there can be more active touches than concurrent touches
        /// supported by the hardware/platform.
        ///
        /// A touch that begins and moves in the same frame will have its <see cref="TouchPhase.Began"/> surface
        /// in that frame and then another entry with <see cref="TouchPhase.Moved"/> and the screen motion
        /// surface in the <em>next</em> frame <em>except</em> if the touch also ended in the frame (in which
        /// case <see cref="phase"/> will be <see cref="TouchPhase.Ended"/> instead of <see cref="TouchPhase.Moved"/>).
        ///
        /// Note that the touches reported by this API do <em>not</em> necessarily have to match the contents of
        /// <see href="https://docs.unity3d.com/ScriptReference/Input-touches.html">UnityEngine.Input.touches</see>.
        /// The reason for this is that the <c>UnityEngine.Input</c> API and the Input System API flush their input
        /// queues at different points in time and may thus have a different view on available input. In particular,
        /// the Input System event queue is flushed <em>later</em> in the frame than inputs for <c>UnityEngine.Input</c>
        /// and may thus have newer inputs available. On Android, for example, touch input is gathered from a separate
        /// UI thread and fed into the input system via a "background" event queue that can gather input asynchronously.
        /// Due to this setup, touch events that will reach <c>UnityEngine.Input</c> only in the next frame may have
        /// already reached the Input System.
        ///
        /// <example>
        /// <code>
        /// void Awake()
        /// {
        ///     // Enable EnhancedTouch.
        ///     EnhancedTouchSupport.Enable();
        /// }
        ///
        /// void Update()
        /// {
        ///     foreach (var touch in Touch.activeTouches)
        ///         if (touch.began)
        ///             Debug.Log($"Touch {touch} started this frame");
        ///         else if (touch.ended)
        ///             Debug.Log($"Touch {touch} ended this frame");
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="InvalidOperationException"><c>EnhancedTouch</c> has not been enabled via <see cref="EnhancedTouchSupport.Enable"/>.</exception>
        /// <seealso cref="activeFingers"/>
        public static ReadOnlyArray<Touch> activeTouches
        {
            get
            {
                EnhancedTouchSupport.CheckEnabled();
                // We lazily construct the array of active touches.
                s_GlobalState.playerState.UpdateActiveTouches();
                return new ReadOnlyArray<Touch>(s_GlobalState.playerState.activeTouches, 0, s_GlobalState.playerState.activeTouchCount);
            }
        }

        /// <summary>
        /// An array of all possible concurrent touch contacts, i.e. all concurrent touch contacts regardless of whether
        /// they are currently active or not.
        /// </summary>
        /// <remarks>
        /// For querying only active fingers, use <see cref="activeFingers"/>.
        ///
        /// The length of this array will always correspond to the maximum number of concurrent touches supported by the system.
        /// Note that the actual number of physically supported concurrent touches as determined by the current hardware and
        /// operating system may be lower than this number.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><c>EnhancedTouch</c> has not been enabled via <see cref="EnhancedTouchSupport.Enable"/>.</exception>
        /// <seealso cref="activeTouches"/>
        /// <seealso cref="activeFingers"/>
        public static ReadOnlyArray<Finger> fingers
        {
            get
            {
                EnhancedTouchSupport.CheckEnabled();
                return new ReadOnlyArray<Finger>(s_GlobalState.playerState.fingers, 0, s_GlobalState.playerState.totalFingerCount);
            }
        }

        /// <summary>
        /// Set of currently active fingers, i.e. touch contacts that currently have an active touch (as defined by <see cref="activeTouches"/>).
        /// </summary>
        /// <exception cref="InvalidOperationException"><c>EnhancedTouch</c> has not been enabled via <see cref="EnhancedTouchSupport.Enable"/>.</exception>
        /// <seealso cref="activeTouches"/>
        /// <seealso cref="fingers"/>
        public static ReadOnlyArray<Finger> activeFingers
        {
            get
            {
                EnhancedTouchSupport.CheckEnabled();
                // We lazily construct the array of active fingers.
                s_GlobalState.playerState.UpdateActiveFingers();
                return new ReadOnlyArray<Finger>(s_GlobalState.playerState.activeFingers, 0, s_GlobalState.playerState.activeFingerCount);
            }
        }

        /// <summary>
        /// Return the set of <see cref="Touchscreen"/>s on which touch input is monitored.
        /// </summary>
        /// <exception cref="InvalidOperationException"><c>EnhancedTouch</c> has not been enabled via <see cref="EnhancedTouchSupport.Enable"/>.</exception>
        public static IEnumerable<Touchscreen> screens
        {
            get
            {
                EnhancedTouchSupport.CheckEnabled();
                return s_GlobalState.touchscreens;
            }
        }

        /// <summary>
        /// Event that is invoked when a finger touches a <see cref="Touchscreen"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"><c>EnhancedTouch</c> has not been enabled via <see cref="EnhancedTouchSupport.Enable"/>.</exception>
        /// <seealso cref="onFingerUp"/>
        /// <seealso cref="onFingerMove"/>
        public static event Action<Finger> onFingerDown
        {
            add
            {
                EnhancedTouchSupport.CheckEnabled();
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onFingerDown.AddCallback(value);
            }
            remove
            {
                EnhancedTouchSupport.CheckEnabled();
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onFingerDown.RemoveCallback(value);
            }
        }

        /// <summary>
        /// Event that is invoked when a finger stops touching a <see cref="Touchscreen"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"><c>EnhancedTouch</c> has not been enabled via <see cref="EnhancedTouchSupport.Enable"/>.</exception>
        /// <seealso cref="onFingerDown"/>
        /// <seealso cref="onFingerMove"/>
        public static event Action<Finger> onFingerUp
        {
            add
            {
                EnhancedTouchSupport.CheckEnabled();
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onFingerUp.AddCallback(value);
            }
            remove
            {
                EnhancedTouchSupport.CheckEnabled();
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onFingerUp.RemoveCallback(value);
            }
        }

        /// <summary>
        /// Event that is invoked when a finger that is in contact with a <see cref="Touchscreen"/> moves
        /// on the screen.
        /// </summary>
        /// <exception cref="InvalidOperationException"><c>EnhancedTouch</c> has not been enabled via <see cref="EnhancedTouchSupport.Enable"/>.</exception>
        /// <seealso cref="onFingerUp"/>
        /// <seealso cref="onFingerDown"/>
        public static event Action<Finger> onFingerMove
        {
            add
            {
                EnhancedTouchSupport.CheckEnabled();
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onFingerMove.AddCallback(value);
            }
            remove
            {
                EnhancedTouchSupport.CheckEnabled();
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onFingerMove.RemoveCallback(value);
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
            get => s_GlobalState.historyLengthPerFinger;

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

            return $"{{id={touchId} finger={finger.index} phase={phase} position={screenPosition} delta={delta} time={time}}}";
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
            Debug.Assert(!s_GlobalState.touchscreens.ContainsReference(screen), "Already added touchscreen");
            s_GlobalState.touchscreens.AppendWithCapacity(screen, capacityIncrement: 5);

            // Add finger tracking to states.
            s_GlobalState.playerState.AddFingers(screen);
#if UNITY_EDITOR
            s_GlobalState.editorState.AddFingers(screen);
#endif
        }

        internal static void RemoveTouchscreen(Touchscreen screen)
        {
            Debug.Assert(s_GlobalState.touchscreens.ContainsReference(screen), "Did not add touchscreen");

            // Remove from list.
            var index = s_GlobalState.touchscreens.IndexOfReference(screen);
            s_GlobalState.touchscreens.RemoveAtWithCapacity(index);

            // Remove fingers from states.
            s_GlobalState.playerState.RemoveFingers(screen);
#if UNITY_EDITOR
            s_GlobalState.editorState.RemoveFingers(screen);
#endif
        }

        ////TODO: only have this hooked when we actually need it
        internal static void BeginUpdate()
        {
#if UNITY_EDITOR
            if ((InputState.currentUpdateType == InputUpdateType.Editor && s_GlobalState.playerState.updateMask != InputUpdateType.Editor) ||
                (InputState.currentUpdateType != InputUpdateType.Editor && s_GlobalState.playerState.updateMask == InputUpdateType.Editor))
            {
                // Either swap in editor state and retain currently active player state in s_EditorState
                // or swap player state back in.
                MemoryHelpers.Swap(ref s_GlobalState.playerState, ref s_GlobalState.editorState);
            }
#endif

            // If we have any touches in activeTouches that are ended or canceled,
            // we need to clear them in the next frame.
            if (s_GlobalState.playerState.haveActiveTouchesNeedingRefreshNextUpdate)
                s_GlobalState.playerState.haveBuiltActiveTouches = false;
        }

        private readonly Finger m_Finger;
        internal InputStateHistory<TouchState>.Record m_TouchRecord;

        /// <summary>
        /// Holds global (static) touch state.
        /// </summary>
        internal struct GlobalState
        {
            internal InlinedArray<Touchscreen> touchscreens;
            internal int historyLengthPerFinger;
            internal CallbackArray<Action<Finger>> onFingerDown;
            internal CallbackArray<Action<Finger>> onFingerMove;
            internal CallbackArray<Action<Finger>> onFingerUp;

            internal FingerAndTouchState playerState;
#if UNITY_EDITOR
            internal FingerAndTouchState editorState;
#endif
        }

        private static GlobalState CreateGlobalState()
        {   // Convenient method since parameterized construction is default
            return new GlobalState { historyLengthPerFinger = 64 };
        }

        internal static GlobalState s_GlobalState = CreateGlobalState();

        internal static ISavedState SaveAndResetState()
        {
            // Save current state
            var savedState = new SavedStructState<GlobalState>(
                ref s_GlobalState,
                (ref GlobalState state) => s_GlobalState = state,
                () => { /* currently nothing to dispose */ });

            // Reset global state
            s_GlobalState = CreateGlobalState();

            return savedState;
        }

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
            public Finger[] fingers;
            public Finger[] activeFingers;
            public Touch[] activeTouches;
            public int activeFingerCount;
            public int activeTouchCount;
            public int totalFingerCount;
            public uint lastId;
            public bool haveBuiltActiveTouches;
            public bool haveActiveTouchesNeedingRefreshNextUpdate;

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
                    activeTouchState = new InputStateHistory<TouchState>
                    {
                        extraMemoryPerRecord = UnsafeUtility.SizeOf<ExtraDataPerTouchState>()
                    };
                }
                else
                {
                    activeTouchState.Clear();
                    activeTouchState.m_ControlCount = 0;
                    activeTouchState.m_Controls.Clear();
                }
                activeTouchCount = 0;
                haveActiveTouchesNeedingRefreshNextUpdate = false;
                var currentUpdateStepCount = InputUpdate.s_UpdateStepCount;

                ////OPTIMIZE: Handle touchscreens that have no activity more efficiently
                ////FIXME: This is sensitive to history size; we probably need to ensure that the Begans and Endeds/Canceleds of touches are always available to us
                ////       (instead of rebuild activeTouches from scratch each time, may be more useful to update it)

                // Go through fingers and for each one, get the touches that were active this update.
                for (var i = 0; i < totalFingerCount; ++i)
                {
                    ref var finger = ref fingers[i];

                    // NOTE: Many of the operations here are inlined in order to not perform the same
                    //       checks/computations repeatedly.

                    var history = finger.m_StateHistory;
                    var touchRecordCount = history.Count;
                    if (touchRecordCount == 0)
                        continue;

                    // We're walking newest-first through the touch history but want the resulting list of
                    // active touches to be oldest first (so that a record for an ended touch comes before
                    // a record of a new touch started on the same finger). To achieve that, we insert
                    // new touch entries for any finger always at the same index (i.e. we prepend rather
                    // than append).
                    var insertAt = activeTouchCount;

                    // Go back in time through the touch records on the finger and collect any touch
                    // active in the current frame. Note that this may yield *multiple* touches for the
                    // finger as there may be touches that have ended in the frame while in the same
                    // frame, a new touch was started.
                    var currentTouchId = 0;
                    var currentTouchState = default(TouchState*);
                    var touchRecordIndex = history.UserIndexToRecordIndex(touchRecordCount - 1); // Start with last record.
                    var touchRecordHeader = history.GetRecordUnchecked(touchRecordIndex);
                    var touchRecordSize = history.bytesPerRecord;
                    var extraMemoryOffset = touchRecordSize - history.extraMemoryPerRecord;
                    for (var n = 0; n < touchRecordCount; ++n)
                    {
                        if (n != 0)
                        {
                            --touchRecordIndex;
                            if (touchRecordIndex < 0)
                            {
                                // We're wrapping around so buffer must be full. Go to last record in buffer.
                                //touchRecordIndex = history.historyDepth - history.m_HeadIndex - 1;
                                touchRecordIndex = history.historyDepth - 1;
                                touchRecordHeader = history.GetRecordUnchecked(touchRecordIndex);
                            }
                            else
                            {
                                touchRecordHeader = (InputStateHistory.RecordHeader*)((byte*)touchRecordHeader - touchRecordSize);
                            }
                        }

                        // Skip if part of an ongoing touch we've already recorded.
                        var touchState = (TouchState*)touchRecordHeader->statePtrWithoutControlIndex; // History is tied to a single TouchControl.
                        var wasUpdatedThisFrame = touchState->updateStepCount == currentUpdateStepCount;
                        if (touchState->touchId == currentTouchId && !touchState->phase.IsEndedOrCanceled())
                        {
                            // If this is the Began record for the touch and that one happened in
                            // the current frame, we force the touch phase to Began.
                            if (wasUpdatedThisFrame && touchState->phase == TouchPhase.Began)
                            {
                                Debug.Assert(currentTouchState != null, "Must have current touch record at this point");

                                currentTouchState->phase = TouchPhase.Began;
                                currentTouchState->position = touchState->position;
                                currentTouchState->delta = default;

                                haveActiveTouchesNeedingRefreshNextUpdate = true;
                            }

                            // Need to continue here as there may still be Ended touches that need to
                            // be taken into account (as in, there may actually be multiple active touches
                            // for the same finger due to how the polling API works).
                            continue;
                        }

                        // If the touch is older than the current frame and it's a touch that has
                        // ended, we don't need to look further back into the history as anything
                        // coming before that will be equally outdated.
                        if (touchState->phase.IsEndedOrCanceled())
                        {
                            // An exception are touches that both began *and* ended in the previous frame.
                            // For these, we surface the Began in the previous update and the Ended in the
                            // current frame.
                            if (!(touchState->beganInSameFrame && touchState->updateStepCount == currentUpdateStepCount - 1) &&
                                !wasUpdatedThisFrame)
                                break;
                        }

                        // Make a copy of the touch so that we can modify data like deltas and phase.
                        // NOTE: Again, not using AddRecord() for speed.
                        // NOTE: Unlike `history`, `activeTouchState` stores control indices as each active touch
                        //       will correspond to a different TouchControl.
                        var touchExtraState = (ExtraDataPerTouchState*)((byte*)touchRecordHeader + extraMemoryOffset);
                        var newRecordHeader = activeTouchState.AllocateRecord(out var newRecordIndex);
                        var newRecordState = (TouchState*)newRecordHeader->statePtrWithControlIndex;
                        var newRecordExtraState = (ExtraDataPerTouchState*)((byte*)newRecordHeader + activeTouchState.bytesPerRecord - UnsafeUtility.SizeOf<ExtraDataPerTouchState>());
                        newRecordHeader->time = touchRecordHeader->time;
                        newRecordHeader->controlIndex = ArrayHelpers.AppendWithCapacity(ref activeTouchState.m_Controls,
                            ref activeTouchState.m_ControlCount, finger.m_StateHistory.controls[0]);

                        UnsafeUtility.MemCpy(newRecordState, touchState, UnsafeUtility.SizeOf<TouchState>());
                        UnsafeUtility.MemCpy(newRecordExtraState, touchExtraState, UnsafeUtility.SizeOf<ExtraDataPerTouchState>());

                        // If the touch hasn't moved this frame, mark it stationary.
                        // EXCEPT: If we are looked at a Moved touch that also began in the same frame and that
                        //         frame is the one immediately preceding us. In that case, we want to surface the Moved
                        //         as if it happened this frame.
                        var phase = touchState->phase;
                        if ((phase == TouchPhase.Moved || phase == TouchPhase.Began) &&
                            !wasUpdatedThisFrame && !(phase == TouchPhase.Moved && touchState->beganInSameFrame && touchState->updateStepCount == currentUpdateStepCount - 1))
                        {
                            newRecordState->phase = TouchPhase.Stationary;
                            newRecordState->delta = default;
                        }
                        // If the touch wasn't updated this frame, zero out its delta.
                        else if (!wasUpdatedThisFrame && !touchState->beganInSameFrame)
                        {
                            newRecordState->delta = default;
                        }
                        else
                        {
                            // We want accumulated deltas only on activeTouches.
                            newRecordState->delta = newRecordExtraState->accumulatedDelta;
                        }

                        var newRecord = new InputStateHistory<TouchState>.Record(activeTouchState, newRecordIndex, newRecordHeader);
                        var newTouch = new Touch(finger, newRecord);

                        ArrayHelpers.InsertAtWithCapacity(ref activeTouches, ref activeTouchCount, insertAt, newTouch);

                        currentTouchId = touchState->touchId;
                        currentTouchState = newRecordState;

                        // For anything but stationary touches on the activeTouches list, we need a subsequent
                        // update in the next frame.
                        if (newTouch.phase != TouchPhase.Stationary)
                            haveActiveTouchesNeedingRefreshNextUpdate = true;
                    }
                }

                haveBuiltActiveTouches = true;
            }
        }

        internal struct ExtraDataPerTouchState
        {
            public Vector2 accumulatedDelta;

            public uint uniqueId; // Unique ID for touch *record* (i.e. multiple TouchStates having the same touchId will still each have a unique ID).

            ////TODO
            //public uint tapCount;
        }
    }
}
