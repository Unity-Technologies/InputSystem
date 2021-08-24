using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.EnhancedTouch
{
    /// <summary>
    /// A source of touches (<see cref="Touch"/>).
    /// </summary>
    /// <remarks>
    /// Each <see cref="Touchscreen"/> has a limited number of fingers it supports corresponding to the total number of concurrent
    /// touches supported by the screen. Unlike a <see cref="Touch"/>, a <see cref="Finger"/> will stay the same and valid for the
    /// lifetime of its <see cref="Touchscreen"/>.
    ///
    /// Note that a Finger does not represent an actual physical finger in the world. That is, the same Finger instance might be used,
    /// for example, for a touch from the index finger at one point and then for a touch from the ring finger. Each Finger simply
    /// corresponds to the Nth touch on the given screen.
    /// </remarks>
    /// <seealso cref="Touch"/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "Holds on to internally managed memory which should not be disposed by the user.")]
    public class Finger
    {
        // This class stores pretty much all the data that is kept by the enhanced touch system. All
        // the finger and history tracking is found here.

        /// <summary>
        /// The screen that the finger is associated with.
        /// </summary>
        /// <value>Touchscreen associated with the touch.</value>
        public Touchscreen screen { get; }

        /// <summary>
        /// Index of the finger on <see cref="screen"/>. Each finger corresponds to the Nth touch on a screen.
        /// </summary>
        public int index { get; }

        /// <summary>
        /// Whether the finger is currently touching the screen.
        /// </summary>
        public bool isActive => currentTouch.valid;

        /// <summary>
        /// The current position of the finger on the screen or <c>default(Vector2)</c> if there is no
        /// ongoing touch.
        /// </summary>
        public Vector2 screenPosition
        {
            get
            {
                ////REVIEW: should this work off of currentTouch instead of lastTouch?
                var touch = lastTouch;
                if (!touch.valid)
                    return default;
                return touch.screenPosition;
            }
        }

        ////REVIEW: should lastTouch and currentTouch have accumulated deltas? would that be confusing?

        /// <summary>
        /// The last touch that happened on the finger or <c>default(Touch)</c> (with <see cref="Touch.valid"/> being
        /// false) if no touch has been registered on the finger yet.
        /// </summary>
        /// <remarks>
        /// A given touch will be returned from this property for as long as no new touch has been started. As soon as a
        /// new touch is registered on the finger, the property switches to the new touch.
        /// </remarks>
        public Touch lastTouch
        {
            get
            {
                var count = m_StateHistory.Count;
                if (count == 0)
                    return default;
                return new Touch(this, m_StateHistory[count - 1]);
            }
        }

        /// <summary>
        /// The currently ongoing touch for the finger or <c>default(Touch)</c> (with <see cref="Touch.valid"/> being false)
        /// if no touch is currently in progress on the finger.
        /// </summary>
        public Touch currentTouch
        {
            get
            {
                var touch = lastTouch;
                if (!touch.valid)
                    return default;
                if (touch.isInProgress)
                    return touch;
                // Ended touches stay current in the frame they ended in.
                if (touch.updateStepCount == InputUpdate.s_UpdateStepCount)
                    return touch;
                return default;
            }
        }

        /// <summary>
        /// The full touch history of the finger.
        /// </summary>
        /// <remarks>
        /// The history is capped at <see cref="Touch.maxHistoryLengthPerFinger"/>. Once full, newer touch records will start
        /// overwriting older entries. Note that this means that a given touch will not trace all the way back to its beginning
        /// if it runs past the max history size.
        /// </remarks>
        public TouchHistory touchHistory => new TouchHistory(this, m_StateHistory);

        internal readonly InputStateHistory<TouchState> m_StateHistory;

        internal Finger(Touchscreen screen, int index, InputUpdateType updateMask)
        {
            this.screen = screen;
            this.index = index;

            // Set up history recording.
            m_StateHistory = new InputStateHistory<TouchState>(screen.touches[index])
            {
                historyDepth = Touch.maxHistoryLengthPerFinger,
                extraMemoryPerRecord = UnsafeUtility.SizeOf<Touch.ExtraDataPerTouchState>(),
                onRecordAdded = OnTouchRecorded,
                onShouldRecordStateChange = ShouldRecordTouch,
                updateMask = updateMask,
            };
            m_StateHistory.StartRecording();

            // record the current state if touch is already in progress
            if (screen.touches[index].isInProgress)
                m_StateHistory.RecordStateChange(screen.touches[index], screen.touches[index].ReadValue());
        }

        private static unsafe bool ShouldRecordTouch(InputControl control, double time, InputEventPtr eventPtr)
        {
            // We only want to record changes that come from events. We ignore internal state
            // changes that Touchscreen itself generates. This includes the resetting of deltas.
            // NOTE: This means we are ignoring delta resets happening in Touchscreen.
            if (!eventPtr.valid)
                return false;

            // Direct memory access for speed.
            var currentTouchState = (TouchState*)((byte*)control.currentStatePtr + control.stateBlock.byteOffset);

            // Touchscreen will record a button down and button up on a TouchControl when a tap occurs.
            // We only want to record the button down, not the button up.
            if (currentTouchState->isTapRelease)
                return false;

            return true;
        }

        private unsafe void OnTouchRecorded(InputStateHistory.Record record)
        {
            var recordIndex = record.recordIndex;
            var touchHeader = m_StateHistory.GetRecordUnchecked(recordIndex);
            var touchState = (TouchState*)touchHeader->statePtrWithoutControlIndex; // m_StateHistory is bound to a single TouchControl.
            touchState->updateStepCount = InputUpdate.s_UpdateStepCount;

            // Invalidate activeTouches.
            Touch.s_GlobalState.playerState.haveBuiltActiveTouches = false;

            // Record the extra data we maintain for each touch.
            var extraData = (Touch.ExtraDataPerTouchState*)((byte*)touchHeader + m_StateHistory.bytesPerRecord -
                UnsafeUtility.SizeOf<Touch.ExtraDataPerTouchState>());
            extraData->uniqueId = ++Touch.s_GlobalState.playerState.lastId;

            // We get accumulated deltas from Touchscreen. Store the accumulated
            // value and "unaccumulate" the value we store on delta.
            extraData->accumulatedDelta = touchState->delta;
            if (touchState->phase != TouchPhase.Began)
            {
                // Inlined (instead of just using record.previous) for speed. Bypassing
                // the safety checks here.
                if (recordIndex != m_StateHistory.m_HeadIndex)
                {
                    var previousRecordIndex = recordIndex == 0 ? m_StateHistory.historyDepth - 1 : recordIndex - 1;
                    var previousTouchHeader = m_StateHistory.GetRecordUnchecked(previousRecordIndex);
                    var previousTouchState = (TouchState*)previousTouchHeader->statePtrWithoutControlIndex;
                    touchState->delta -= previousTouchState->delta;
                    touchState->beganInSameFrame = previousTouchState->beganInSameFrame &&
                        previousTouchState->updateStepCount == touchState->updateStepCount;
                }
            }
            else
            {
                touchState->beganInSameFrame = true;
            }

            // Trigger callback.
            switch (touchState->phase)
            {
                case TouchPhase.Began:
                    DelegateHelpers.InvokeCallbacksSafe(ref Touch.s_GlobalState.onFingerDown, this, "Touch.onFingerDown");
                    break;
                case TouchPhase.Moved:
                    DelegateHelpers.InvokeCallbacksSafe(ref Touch.s_GlobalState.onFingerMove, this, "Touch.onFingerMove");
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    DelegateHelpers.InvokeCallbacksSafe(ref Touch.s_GlobalState.onFingerUp, this, "Touch.onFingerUp");
                    break;
            }
        }

        private unsafe Touch FindTouch(uint uniqueId)
        {
            Debug.Assert(uniqueId != default, "0 is not a valid ID");
            foreach (var record in m_StateHistory)
            {
                if (((Touch.ExtraDataPerTouchState*)record.GetUnsafeExtraMemoryPtrUnchecked())->uniqueId == uniqueId)
                    return new Touch(this, record);
            }

            return default;
        }

        internal unsafe TouchHistory GetTouchHistory(Touch touch)
        {
            Debug.Assert(touch.finger == this);

            // If the touch is not pointing to our history, it's probably a touch we copied for
            // activeTouches. We know the unique ID of the touch so go and try to find the touch
            // in our history.
            var touchRecord = touch.m_TouchRecord;
            if (touchRecord.owner != m_StateHistory)
            {
                touch = FindTouch(touch.uniqueId);
                if (!touch.valid)
                    return default;
            }

            var touchId = touch.touchId;
            var startIndex = touch.m_TouchRecord.index;

            // If the current touch isn't the beginning of the touch, search back through the
            // history for all touches belonging to the same contact.
            var count = 0;
            if (touch.phase != TouchPhase.Began)
            {
                for (var previousRecord = touch.m_TouchRecord.previous; previousRecord.valid; previousRecord = previousRecord.previous)
                {
                    var touchState = (TouchState*)previousRecord.GetUnsafeMemoryPtr();

                    // Stop if the touch doesn't belong to the same contact.
                    if (touchState->touchId != touchId)
                        break;
                    ++count;

                    // Stop if we've found the beginning of the touch.
                    if (touchState->phase == TouchPhase.Began)
                        break;
                }
            }

            if (count == 0)
                return default;

            // We don't want to include the touch we started with.
            --startIndex;

            return new TouchHistory(this, m_StateHistory, startIndex, count);
        }
    }
}
