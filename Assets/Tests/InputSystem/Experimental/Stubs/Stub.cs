using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem
{
    internal readonly struct Stub<T> where T : struct
    {
        private readonly Stream<T> m_Stream;

        public Stub(Stream<T> stream)
        {
            m_Stream = stream;
        }

        public void Change(T value)
        {
            m_Stream.OfferByValue(value);
        }
        
        public void Change(ref T value)
        {
            m_Stream.OfferByRef(ref value);
        }
    }
}