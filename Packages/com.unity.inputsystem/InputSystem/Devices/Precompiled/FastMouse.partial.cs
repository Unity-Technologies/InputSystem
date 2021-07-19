using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem
{
    internal partial class FastMouse : IInputStateCallbackReceiver
    {
        protected new void OnNextUpdate()
        {
            // Changing these separately seems to not result in much of a difference
            // compared to just doing an InputState.Change with a complete MouseState.
            InputState.Change(delta, Vector2.zero);
            InputState.Change(scroll, Vector2.zero);
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

            InputState.Change(this, ref newState, eventPtr: eventPtr);
        }

        void IInputStateCallbackReceiver.OnNextUpdate()
        {
            OnNextUpdate();
        }

        void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
        {
            OnStateEvent(eventPtr);
        }
    }
}
