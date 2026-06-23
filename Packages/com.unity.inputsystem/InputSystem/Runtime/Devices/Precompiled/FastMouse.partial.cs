using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem
{
    internal partial class FastMouse : IInputStateCallbackReceiver, IEventMerger
    {
        protected new void OnNextUpdate()
        {
            // Changing these separately seems to not result in much of a difference
            // compared to just doing an InputState.Change with a complete MouseState.
            InputState.Change(delta, Vector2.zero, InputState.currentUpdateType);
            InputState.Change(scroll, Vector2.zero, InputState.currentUpdateType);
        }

        // For FastMouse, we know that our layout is MouseState so we can just go directly
        // to memory.

        protected new unsafe void OnStateEvent(InputEventPtr eventPtr)
        {
            MouseState newState;

            if (eventPtr.type == StateEvent.Type)
            {
                var stateEvent = StateEvent.FromUnchecked(eventPtr);
                if (stateEvent->stateFormat != MouseState.Format)
                {
                    base.OnStateEvent(eventPtr);
                    return;
                }
                newState = *(MouseState*)stateEvent->state;
            }
            else if (eventPtr.type == DeltaStateEvent.Type)
            {
                var deltaEvent = DeltaStateEvent.FromUnchecked(eventPtr);
                if (!IsFullMouseStateDeltaEvent(deltaEvent))
                {
                    base.OnStateEvent(eventPtr);
                    return;
                }
                newState = *(MouseState*)deltaEvent->deltaState;
            }
            else
            {
                base.OnStateEvent(eventPtr);
                return;
            }

            var stateFromDevice = (MouseState*)((byte*)currentStatePtr + m_StateBlock.byteOffset);

            newState.delta += stateFromDevice->delta;
            newState.scroll += stateFromDevice->scroll;

            InputState.Change(this, ref newState, InputState.currentUpdateType, eventPtr: eventPtr);
        }

        void IInputStateCallbackReceiver.OnNextUpdate()
        {
            OnNextUpdate();
        }

        void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
        {
            OnStateEvent(eventPtr);
        }

        internal static unsafe bool MergeForward(InputEventPtr currentEventPtr, InputEventPtr nextEventPtr)
        {
            MouseState* currentState;
            MouseState* nextState;

            if (currentEventPtr.type == StateEvent.Type && nextEventPtr.type == StateEvent.Type)
            {
                var currentEvent = StateEvent.FromUnchecked(currentEventPtr);
                var nextEvent = StateEvent.FromUnchecked(nextEventPtr);
                if (currentEvent->stateFormat != MouseState.Format || nextEvent->stateFormat != MouseState.Format)
                    return false;
                currentState = (MouseState*)currentEvent->state;
                nextState = (MouseState*)nextEvent->state;
            }
            else if (currentEventPtr.type == DeltaStateEvent.Type && nextEventPtr.type == DeltaStateEvent.Type)
            {
                var currentEvent = DeltaStateEvent.FromUnchecked(currentEventPtr);
                var nextEvent = DeltaStateEvent.FromUnchecked(nextEventPtr);
                if (!IsFullMouseStateDeltaEvent(currentEvent) || !IsFullMouseStateDeltaEvent(nextEvent))
                    return false;
                currentState = (MouseState*)currentEvent->deltaState;
                nextState = (MouseState*)nextEvent->deltaState;
            }
            else if (currentEventPtr.type == StateEvent.Type && nextEventPtr.type == DeltaStateEvent.Type)
            {
                var currentEvent = StateEvent.FromUnchecked(currentEventPtr);
                var nextEvent = DeltaStateEvent.FromUnchecked(nextEventPtr);
                if (currentEvent->stateFormat != MouseState.Format || !IsFullMouseStateDeltaEvent(nextEvent))
                    return false;
                currentState = (MouseState*)currentEvent->state;
                nextState = (MouseState*)nextEvent->deltaState;
            }
            else if (currentEventPtr.type == DeltaStateEvent.Type && nextEventPtr.type == StateEvent.Type)
            {
                var currentEvent = DeltaStateEvent.FromUnchecked(currentEventPtr);
                var nextEvent = StateEvent.FromUnchecked(nextEventPtr);
                if (!IsFullMouseStateDeltaEvent(currentEvent) || nextEvent->stateFormat != MouseState.Format)
                    return false;
                currentState = (MouseState*)currentEvent->deltaState;
                nextState = (MouseState*)nextEvent->state;
            }
            else
            {
                return false;
            }

            // if buttons or clickCount changed we need to process it, so don't merge events together
            if (currentState->buttons != nextState->buttons || currentState->clickCount != nextState->clickCount)
                return false;

            nextState->delta += currentState->delta;
            nextState->scroll += currentState->scroll;
            return true;
        }

        bool IEventMerger.MergeForward(InputEventPtr currentEventPtr, InputEventPtr nextEventPtr)
        {
            return MergeForward(currentEventPtr, nextEventPtr);
        }

        // A DeltaStateEvent qualifies as a full MouseState when it starts at offset 0 and covers
        // at least enough bytes to read position, delta, scroll, buttons, and clickCount.
        private static unsafe bool IsFullMouseStateDeltaEvent(DeltaStateEvent* deltaEvent)
        {
            return deltaEvent->stateFormat == MouseState.Format
                && deltaEvent->stateOffset == 0
                && deltaEvent->deltaStateSizeInBytes >= (uint)sizeof(MouseState);
        }
    }
}
