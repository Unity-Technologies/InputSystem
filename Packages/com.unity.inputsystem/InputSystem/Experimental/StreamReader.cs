using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    public struct StreamReader<T> : IEnumerable<T> where T : struct
    {
        private readonly Stream<T> m_Stream;
        private readonly int m_Offset;

        public StreamReader(Stream<T> stream, int offset)
        {
            m_Stream = stream;
            m_Offset = offset;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly StreamReader<T> m_Reader;
            private int m_Index;

            public Enumerator(StreamReader<T> reader, int index = 0)
            {
                m_Reader = reader;
                m_Index = index;
                Current = default;
            }

            public void Dispose()
            {
                // TODO release managed resources here
            }

            public bool MoveNext()
            {
                ++m_Index;
                return false;
            }

            public void Reset()
            {
                m_Index = 0;
            }

            public T Current { get; }

            object IEnumerator.Current => Current;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
