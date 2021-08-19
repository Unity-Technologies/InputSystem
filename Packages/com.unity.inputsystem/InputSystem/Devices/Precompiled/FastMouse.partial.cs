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
            if (eventPtr.type != StateEvent.Type)
            {
                base.OnStateEvent(eventPtr);
                return;
            }

            var stateEvent = StateEvent.FromUnchecked(eventPtr);
            if (stateEvent->stateFormat != MouseState.Format)
            {
                base.OnStateEvent(eventPtr);
                return;
            }

            var newState = *(MouseState*)stateEvent->state;
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
            if (currentEventPtr.type != StateEvent.Type || nextEventPtr.type != StateEvent.Type)
                return false;

            var currentEvent = StateEvent.FromUnchecked(currentEventPtr);
            var nextEvent = StateEvent.FromUnchecked(nextEventPtr);

            if (currentEvent->stateFormat != MouseState.Format || nextEvent->stateFormat != MouseState.Format)
                return false;

            var currentState = (MouseState*)currentEvent->state;
            var nextState = (MouseState*)nextEvent->state;

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
    }
}
