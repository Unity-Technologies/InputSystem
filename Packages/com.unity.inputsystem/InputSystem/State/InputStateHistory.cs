using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

////REVIEW: What about instead of capturing a control directly, this works on bindings instead?
////        This would still allow tracing individual controls but at the same time would allow
////        capturing history including procesors and composites and everything (and multiple
////        controls at the same time)

////TODO: allow correlating history to frames/updates

namespace UnityEngine.Experimental.Input
{
    public class InputStateHistory
    {
        public InputStateHistory(InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException("control");
            this.control = control;
        }

        public IntPtr GetStatePtr(int index)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        protected void Destroy()
        {
            if (m_StateBuffer.Length > 0)
                m_StateBuffer.Dispose();

            m_StateBuffer = new NativeArray<byte>();
            m_Head = 0;
            m_Tail = 0;
        }

        ////REVIEW: make control settable?
        public InputControl control { get; protected set; }

        public int historySize { get; set; }

        public int Count { get; private set; }

        private NativeArray<byte> m_StateBuffer;
        private int m_Head;
        private int m_Tail;
    }

    /// <summary>
    /// Records value changes of a given control over time.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class InputStateHistory<TValue> : InputStateHistory, IReadOnlyList<TValue>, IDisposable
        where TValue : struct
    {
        public InputStateHistory(InputControl<TValue> control)
            : base(control)
        {
        }

        ~InputStateHistory()
        {
            Destroy();
        }

        public void Enable()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TValue this[int index]
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}
