using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.LowLevel;

////TODO: invalidate data when associated actions re-resolve

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Records the triggering of actions into <see cref="ActionEvent">action events</see>.
    /// </summary>
    /// <remarks>
    /// This is an alternate way to the callback-based responses (such as <see cref="InputAction.performed"/>)
    /// of <see cref="InputAction">input actions</see>. Instead of executing response code right away whenever
    /// an action triggers, an event is recorded which can then be queried on demand.
    /// </remarks>
    public class InputActionEventQueue : IInputActionCallbackReceiver, IEnumerable<InputActionEventQueue.ActionEventPtr>, IDisposable
    {
        /// <summary>
        /// Directly access the underlying raw memory queue.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public InputEventBuffer buffer
        {
            get { throw new NotImplementedException(); }
        }

        public int count
        {
            get { return m_EventBuffer.eventCount; }
        }

        public IEnumerator<ActionEventPtr> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Record the triggering of an action as an <see cref="ActionEvent">action event</see>.
        /// </summary>
        /// <param name="context"></param>
        public void OnActionTriggered(InputAction.CallbackContext context)
        {
            //determine size of value
            //allocate event
            //fill out fields
            //read value into event

            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        private InputEventBuffer m_EventBuffer;

        /// <summary>
        /// A wrapper around <see cref="ActionEvent"/> that automatically translates all the
        /// information in events into their high-level representations.
        /// </summary>
        /// <remarks>
        /// For example, instead of returning <see cref="ActionEvent.controlIndex">control indices</see>,
        /// it automatically resolves and returns the respective <see cref="InputControl">controls</see>.
        /// </remarks>
        public struct ActionEventPtr
        {
            public InputAction action
            {
                get { throw new NotImplementedException(); }
            }

            public InputActionPhase phase
            {
                get { throw new NotImplementedException(); }
            }

            public InputControl control
            {
                get { throw new NotImplementedException(); }
            }

            public double time
            {
                get { throw new NotImplementedException(); }
            }

            public TValue ReadValue<TValue>()
            {
                throw new NotImplementedException();
            }
        }

        internal struct Enumerator : IEnumerator<ActionEventPtr>
        {
            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public ActionEventPtr Current { get; private set; }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
    }
}
