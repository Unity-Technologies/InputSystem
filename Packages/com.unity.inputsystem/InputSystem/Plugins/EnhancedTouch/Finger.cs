using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.EnhancedTouch
{
    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// Note that in general there is no ability to detect actual physical fingers. This means that a finger
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "Holds on to internally managed memory which should not be disposed by the user.")]
    public class Finger
    {
        // This class stores pretty much all the data that is kept by the enhanced touch system. All
        // the finger and history tracking is found here.

        public Touchscreen screen { get; }
        public int index { get; }

        /// <summary>
        /// Whether the finger is currently touching the screen.
        /// </summary>
        public bool isActive => currentTouch.valid;

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
                if (touch.updateStepCount == Touch.s_ActiveState.updateStepCount)
                    return touch;
                return default;
            }
        }

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
        }

        private static bool ShouldRecordTouch(InputControl control, double time, InputEventPtr eventPtr)
        {
            // We only want to record changes that come from events. We ignore internal state
            // changes that Touchscreen itself generates. This includes the resetting of deltas.
            // NOTE: This means we are ignoring delta resets happening in Touchscreen.
            if (!eventPtr.valid)
                return false;

            var touchControl = (TouchControl)control;
            var touch = touchControl.ReadValue();

            // Touchscreen will record a button down and button up a TouchControl when a tap occurs.
            // We only want to record the button down, not the button up.
            if (touch.phase == TouchPhase.Ended && !touch.isTap && touch.tapCount > 0)
                return false;

            return true;
        }

        private unsafe void OnTouchRecorded(InputStateHistory.Record record)
        {
            var touchState = (TouchState*)record.GetUnsafeMemoryPtr();
            Touch.s_ActiveState.haveBuiltActiveTouches = false;

            // Record the extra data we maintain for each touch.
            var extraData = (Touch.ExtraDataPerTouchState*)record.GetUnsafeExtraMemoryPtr();
            extraData->updateStepCount = Touch.s_ActiveState.updateStepCount;
            extraData->uniqueId = ++Touch.s_ActiveState.lastId;

            // We get accumulated deltas from Touchscreen. Store the accumulated
            // value and "unaccumulate" the value we store on delta.
            extraData->accumulatedDelta = touchState->delta;
            if (touchState->phase != TouchPhase.Began)
            {
                var previous = record.previous;
                if (previous.valid)
                    touchState->delta -= ((TouchState*)previous.GetUnsafeMemoryPtr())->delta;
            }

            // Trigger callback.
            var statePtr = (TouchState*)record.GetUnsafeMemoryPtr();
            switch (statePtr->phase)
            {
                case TouchPhase.Began:
                    DelegateHelpers.InvokeCallbacksSafe(ref Touch.s_OnFingerDown, this, "Touch.onFingerDown");
                    break;
                case TouchPhase.Moved:
                    DelegateHelpers.InvokeCallbacksSafe(ref Touch.s_OnFingerMove, this, "Touch.onFingerMove");
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    DelegateHelpers.InvokeCallbacksSafe(ref Touch.s_OnFingerUp, this, "Touch.onFingerUp");
                    break;
            }
        }

        private unsafe Touch FindTouch(uint uniqueId)
        {
            Debug.Assert(uniqueId != default, "0 is not a valid ID");
            foreach (var record in m_StateHistory)
            {
                if (((Touch.ExtraDataPerTouchState*)record.GetUnsafeExtraMemoryPtr())->uniqueId == uniqueId)
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
