using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Input.Utilities;

////TODO: allow correlating history to frames/updates

#pragma warning disable 0649
namespace UnityEngine.Experimental.Input
{
    public class InputHistory : IDisposable, IEnumerable<InputHistory.Value>
    {
        public const int kDefaultHistorySize = 100;

        public InputHistory(string path)
        {
        }

        public InputHistory(InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
        }

        ~InputHistory()
        {
            Dispose();
        }

        public IntPtr GetStatePtr(int index)
        {
            throw new NotImplementedException();
        }

        public void Enable()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<Value> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        protected void Destroy()
        {
            if (m_StateBuffer.IsCreated)
                m_StateBuffer.Dispose();

            m_StateBuffer = new NativeArray<byte>();
        }

        public ReadOnlyArray<InputControl> controls => new ReadOnlyArray<InputControl>(m_Controls);

        public int historySize { get; set; }

        public int Count { get; private set; }

        private InputControl[] m_Controls;
        private NativeArray<byte> m_StateBuffer;
        private int m_StateSizeInBytes;
        private int m_StateCount;

        public struct Value
        {
            public InputUpdateType updateType;
            public int frame;
            public double time;
        }
    }

    /// <summary>
    /// Records value changes of a given control over time.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class InputHistory<TValue> : InputHistory, IReadOnlyList<TValue>
        where TValue : struct
    {
        public InputHistory(InputControl<TValue> control)
            : base(control)
        {
        }

        public InputHistory(string path)
            : base(path)
        {
        }

        ~InputHistory()
        {
            Destroy();
        }

        public new IEnumerator<TValue> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TValue this[int index]
        {
            get { throw new NotImplementedException(); }
        }

        public struct Enumerator : IEnumerator<TValue>
        {
            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public TValue Current { get; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }

        public struct Value<Tvalue>
        {
        }
    }
}
