using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem
{
    internal partial class FastMouse : IInputStateCallbackReceiver, IFlushable
    {
        protected new void OnNextUpdate()
        {
            // Changing these separately seems to not result in much of a difference
            // compared to just doing an InputState.Change with a complete MouseState.
            InputState.Change(delta, Vector2.zero, InputState.currentUpdateType);
            InputState.Change(scroll, Vector2.zero, InputState.currentUpdateType);

            m_AccumulatedDelta = Vector2.zero;
            m_AccumulatedScroll = Vector2.zero;
            m_DisableMouseMoveCompression = InputSystem.settings.disableMouseMoveCompression;
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

            if (m_DisableMouseMoveCompression)
            {
                var stateFromDevice = (MouseState*)((byte*)currentStatePtr + m_StateBlock.byteOffset);
                newState.delta += stateFromDevice->delta;
                newState.scroll += stateFromDevice->scroll;
                InputState.Change(this, ref newState, InputState.currentUpdateType, eventPtr: eventPtr);
                return;
            }

            m_AccumulatedDelta += newState.delta;
            m_AccumulatedScroll += newState.scroll;

            if (newState.buttons == m_PreviousEventState.buttons &&
                newState.clickCount == m_PreviousEventState.clickCount)
            {
                m_LastEventPtr = eventPtr;
            }
            else
            {
                var state = new MouseState
                {
                    position = newState.position,
                    delta = m_AccumulatedDelta,
                    scroll = m_AccumulatedScroll,
                    buttons = newState.buttons,
                    clickCount = newState.clickCount
                };
                InputState.Change(this, ref state, InputState.currentUpdateType, eventPtr);

                // this state has been handled now, so set m_LastEventPtr to null because there's nothing to flush later
                m_LastEventPtr = null;
            }

            m_PreviousEventState = newState;
        }

        public void Flush()
        {
            if (m_LastEventPtr == null)
                return;

            var state = new MouseState
            {
                position = m_PreviousEventState.position,
                delta = m_AccumulatedDelta,
                scroll = m_AccumulatedScroll,
                buttons = m_PreviousEventState.buttons,
                clickCount = m_PreviousEventState.clickCount
            };
            InputState.Change(this, ref state, InputState.currentUpdateType, m_LastEventPtr);
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

        /// <summary>
        /// When mouse move compression is turned on, these variables are used to store accumulated
        /// delta and scroll values during the frame. Without compression, the device state is
        /// updated for every event, and so the accumulated values are simply store in there.
        /// </summary>
        private Vector2 m_AccumulatedDelta;
        private Vector2 m_AccumulatedScroll;

        /// <summary>
        /// Stores the previous mouse event state so it can be applied in the Flush method, and also
        /// so button and click count states can be tracked across frames.
        /// </summary>
        private MouseState m_PreviousEventState;

        /// <summary>
        /// When mouse move compression is turned on, it is necessary to store the last event
        /// received in case the last events in the frame are all mouse moves. This is because
        /// the OnStateEvent method has no idea if it is processing the last event of the frame
        /// or not, and so can't know to update the device state. Instead, the Flush method
        /// will be called when all events have been processed, and the device state will be
        /// updated from this event.
        /// </summary>
        private InputEventPtr m_LastEventPtr;
        private bool m_DisableMouseMoveCompression;
    }
}
