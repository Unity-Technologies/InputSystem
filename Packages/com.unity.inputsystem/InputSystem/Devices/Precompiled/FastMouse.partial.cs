using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem
{
    internal partial class FastMouse : IInputStateCallbackReceiver, IFlushableInputDevice
    {
        protected new void OnNextUpdate()
        {
            // Changing these separately seems to not result in much of a difference
            // compared to just doing an InputState.Change with a complete MouseState.
            InputState.Change(delta, Vector2.zero, InputState.currentUpdateType);
            InputState.Change(scroll, Vector2.zero, InputState.currentUpdateType);
            m_LastState = default;
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

            if (InputSystem.settings.disableMouseMoveCompression)
            {
	            var stateFromDevice = (MouseState*)((byte*)currentStatePtr + m_StateBlock.byteOffset);
	            newState.delta += stateFromDevice->delta;
	            newState.scroll += stateFromDevice->scroll;
	            InputState.Change(this, ref newState, InputState.currentUpdateType, eventPtr: eventPtr);
	            return;
            }

            m_LastState.position = newState.position;

            if (newState.buttons == m_LastState.buttons &&
                newState.clickCount == m_LastState.clickCount)
            {
	            m_LastState.delta += newState.delta;
	            m_LastState.scroll += newState.scroll;
	            m_LastEventPtr = eventPtr;
            }
            else
            {
	            InputState.Change(this, ref m_LastState, InputState.currentUpdateType, m_LastEventPtr);

                newState.delta += m_LastState.delta;
	            newState.scroll += m_LastState.scroll;
				InputState.Change(this, ref newState, InputState.currentUpdateType, eventPtr);

				m_LastEventPtr = null;
                m_LastState = default;
            }
        }

        public void Flush()
        {
	        if (m_LastEventPtr == null)
		        return;

	        InputState.Change(this, ref m_LastState, InputState.currentUpdateType, m_LastEventPtr);
	        m_LastEventPtr = null;
        }

        void IInputStateCallbackReceiver.OnNextUpdate()
        {
            OnNextUpdate();
        }

        void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
        {
            OnStateEvent(eventPtr);
        }

        private MouseState m_LastState;
        private InputEventPtr m_LastEventPtr;
    }

    internal interface IFlushableInputDevice
    {
	    void Flush();
    }
}
