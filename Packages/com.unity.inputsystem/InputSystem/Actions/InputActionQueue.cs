using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: invalidate data when associated actions re-resolve

////TODO: add random access capability

////REVIEW: rename back to InputActionEventQueue? if we have InputActionMap, InputActionStack, and InputActionQueue, it's
////        rather confusing that in the end, they contain very different action-related things

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Records the triggering of actions into a sequence of events that can be replayed at will.
    /// </summary>
    /// <remarks>
    /// This is an alternate way to the callback-based responses (such as <see cref="InputAction.performed"/>)
    /// of <see cref="InputAction">input actions</see>. Instead of executing response code right away whenever
    /// an action triggers, an <see cref="RecordAction">event is recorded</see> which can then be queried on demand.
    /// </remarks>
    public class InputActionQueue : IEnumerable<InputActionQueue.ActionEventPtr>, IDisposable
    {
        ////REVIEW: this is of limited use without having access to ActionEvent
        /// <summary>
        /// Directly access the underlying raw memory queue.
        /// </summary>
        public InputEventBuffer buffer
        {
            get { return m_EventBuffer; }
        }

        public int count
        {
            get { return m_EventBuffer.eventCount; }
        }

        /// <summary>
        /// Record the triggering of an action as an <see cref="ActionEventPtr">action event</see>.
        /// </summary>
        /// <param name="context"></param>
        /// <see cref="InputAction.performed"/>
        /// <see cref="InputAction.started"/>
        /// <see cref="InputAction.cancelled"/>
        /// <see cref="InputActionMap.actionTriggered"/>
        public unsafe void RecordAction(InputAction.CallbackContext context)
        {
            // Find/add state.
            var stateIndex = m_ActionMapStates.IndexOf(context.m_State);
            if (stateIndex == -1)
                stateIndex = m_ActionMapStates.AppendWithCapacity(context.m_State);

            // Allocate event.
            var valueSizeInBytes = context.valueSizeInBytes;
            var eventPtr =
                (ActionEvent*)m_EventBuffer.AllocateEvent(ActionEvent.GetEventSizeWithValueSize(valueSizeInBytes));

            // Initialize event.
            eventPtr->baseEvent.type = ActionEvent.Type;
            eventPtr->baseEvent.time = context.time;
            eventPtr->stateIndex = stateIndex;
            eventPtr->controlIndex = context.m_ControlIndex;
            eventPtr->bindingIndex = context.m_BindingIndex;
            eventPtr->interactionIndex = context.m_InteractionIndex;
            eventPtr->startTime = context.startTime;
            eventPtr->phase = context.phase;

            // Store value.
            var valueBuffer = eventPtr->valueData;
            context.ReadValue(valueBuffer, valueSizeInBytes);
        }

        public void Clear()
        {
            m_EventBuffer.Reset();
            m_ActionMapStates.ClearWithCapacity();
        }

        public void Dispose()
        {
            m_EventBuffer.Dispose();
            m_ActionMapStates.Clear();
        }

        public IEnumerator<ActionEventPtr> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private InputEventBuffer m_EventBuffer;
        private InlinedArray<InputActionMapState> m_ActionMapStates;

        /// <summary>
        /// A wrapper around <see cref="ActionEvent"/> that automatically translates all the
        /// information in events into their high-level representations.
        /// </summary>
        /// <remarks>
        /// For example, instead of returning <see cref="ActionEvent.controlIndex">control indices</see>,
        /// it automatically resolves and returns the respective <see cref="InputControl">controls</see>.
        /// </remarks>
        public unsafe struct ActionEventPtr
        {
            private InputActionMapState m_State;
            private ActionEvent* m_Ptr;

            internal ActionEventPtr(InputActionMapState state, ActionEvent* eventPtr)
            {
                m_State = state;
                m_Ptr = eventPtr;
            }

            public InputAction action
            {
                get { return m_State.GetActionOrNull(m_Ptr->bindingIndex); }
            }

            public InputActionPhase phase
            {
                get { return m_Ptr->phase; }
            }

            public InputControl control
            {
                get { return m_State.controls[m_Ptr->controlIndex]; }
            }

            public IInputInteraction interaction
            {
                get
                {
                    var index = m_Ptr->interactionIndex;
                    if (index == InputActionMapState.kInvalidIndex)
                        return null;

                    return m_State.interactions[index];
                }
            }

            public double time
            {
                get { return m_Ptr->baseEvent.time; }
            }

            public double startTime
            {
                get { return m_Ptr->startTime; }
            }

            public double duration
            {
                get { return time - startTime; }
            }

            public int valueSizeInBytes
            {
                get { return m_Ptr->valueSizeInBytes; }
            }

            public void ReadValue(void* buffer, int bufferSize)
            {
                throw new NotImplementedException();
            }

            public TValue ReadValue<TValue>()
                where TValue : struct
            {
                var valueSizeInBytes = m_Ptr->valueSizeInBytes;

                ////REVIEW: do we want more checking than this?
                if (UnsafeUtility.SizeOf<TValue>() != valueSizeInBytes)
                    throw new InvalidOperationException(string.Format(
                        "Cannot read a value of type '{0}' with size {1} from event on action '{2}' with value size {3}",
                        typeof(TValue).Name, UnsafeUtility.SizeOf<TValue>(), action, valueSizeInBytes));

                var result = new TValue();
                var resultPtr = UnsafeUtility.AddressOf(ref result);
                UnsafeUtility.MemCpy(resultPtr, m_Ptr->valueData, valueSizeInBytes);

                return result;
            }
        }

        internal unsafe struct Enumerator : IEnumerator<ActionEventPtr>
        {
            private InputActionQueue m_Queue;
            private ActionEvent* m_Buffer;
            private ActionEvent* m_CurrentEvent;
            private int m_CurrentIndex;
            private int m_EventCount;

            public Enumerator(InputActionQueue queue)
            {
                m_Queue = queue;
                m_Buffer = (ActionEvent*)queue.m_EventBuffer.bufferPtr.ToPointer();
                m_EventCount = queue.m_EventBuffer.eventCount;
                m_CurrentEvent = null;
                m_CurrentIndex = 0;
            }

            public bool MoveNext()
            {
                if (m_CurrentIndex == m_EventCount)
                    return false;

                if (m_CurrentEvent == null)
                {
                    m_CurrentEvent = m_Buffer;
                    return m_CurrentEvent != null;
                }

                Debug.Assert(m_CurrentEvent != null);

                ++m_CurrentIndex;
                if (m_CurrentIndex == m_EventCount)
                    return false;

                m_CurrentEvent = (ActionEvent*)InputEvent.GetNextInMemory((InputEvent*)m_CurrentEvent);
                return true;
            }

            public void Reset()
            {
                m_CurrentEvent = null;
                m_CurrentIndex = 0;
            }

            public void Dispose()
            {
            }

            public ActionEventPtr Current
            {
                get
                {
                    var state = m_Queue.m_ActionMapStates[m_CurrentEvent->stateIndex];
                    return new ActionEventPtr(state, m_CurrentEvent);
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}
