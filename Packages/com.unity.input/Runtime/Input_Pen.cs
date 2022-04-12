using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// NOTE:
// For some reason, the pen API that was recently added to UnityEngine.Input is implemented in *both* the old
// and the new backends.

// REVIEW: Do we really want to keep this API or just axe it going forward? IMO given this seems to have been added
//         solely for the needs of UITK, we should kill it.

namespace UnityEngine
{
    public static partial class Input
    {
        public static int penEventCount => s_Pen.history?.Count ?? 0;

        public static PenData GetPenEvent(int index)
        {
            if (index < 0 || index >= penEventCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return s_Pen.history[index].ToPenData();
        }

        public static void ResetPenEvents()
        {
            s_Pen.history.Clear();
        }

        public static PenData GetLastPenContactEvent()
        {
            return s_Pen.lastContact;
        }

        public static void ClearLastPenContactEvent()
        {
            s_Pen.lastContact.contactType = PenEventType.NoContact;
        }

        private struct PenInfo
        {
            public bool isDown;
            public InputStateHistory history;
            public PenData lastContact;

            public void Cleanup()
            {
                history?.Dispose();
                history = null;
            }

            public void NextFrame()
            {
                history?.Clear();
            }
        }

        private static PenInfo s_Pen;

        private static unsafe void AddPen(Pen pen)
        {
            ////TODO: at the moment we only support pen's using PenState
            if (pen.stateBlock.format != PenState.Format)
                return;

            if (s_Pen.history == null)
                s_Pen.history = new InputStateHistory((int)pen.stateBlock.alignedSizeInBytes);

            InputState.AddChangeMonitor(pen, (control, time, eventPtr, _) =>
            {
                ////TODO: handle delta events and different state formats
                if (!eventPtr.IsA<StateEvent>())
                    return;

                ////FIXME: Like in the current native implementation, this will all go nicely wrong if there's multiple pens...

                var stateEvent = StateEvent.FromUnchecked(eventPtr);
                if (stateEvent->stateFormat != PenState.Format)
                    return;

                var statePtr = (PenState*)StateEvent.From(eventPtr)->state - control.device.stateBlock.byteOffset;
                var record = s_Pen.history.RecordStateChange(pen, statePtr, eventPtr.time);

                var isDown = statePtr->IsButtonDown(PenButton.Tip) && !s_Pen.isDown;
                var isUp = !isDown && statePtr->IsButtonDown(PenButton.Tip) && s_Pen.isDown;
                if (isDown || isUp)
                {
                    s_Pen.lastContact = record.ToPenData();
                    s_Pen.lastContact.contactType = isDown ? PenEventType.PenDown : PenEventType.PenUp;
                }

                s_Pen.isDown = statePtr->IsButtonDown(PenButton.Tip);
            });
        }

        private static unsafe PenData ToPenData(this InputStateHistory.Record record)
        {
            var penStatePtr = (PenState*)record.GetUnsafeMemoryPtrUnchecked();
            return new PenData
            {
                position = penStatePtr->position,
                deltaPos = penStatePtr->delta,
                pressure = penStatePtr->pressure,
                tilt = penStatePtr->tilt,
                twist = penStatePtr->twist,
                penStatus = (penStatePtr->IsButtonDown(PenButton.Tip) ? PenStatus.Contact : 0)
                    | (penStatePtr->IsButtonDown(PenButton.Barrel1) ? PenStatus.Barrel : 0)
                    | (penStatePtr->IsButtonDown(PenButton.Eraser) ? PenStatus.Eraser : 0),
                // Looking at the Windows code for the pen API, it seems contact isn't filled out.
            };
        }
    }
}
