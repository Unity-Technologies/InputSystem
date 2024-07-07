using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem
{
    internal readonly struct ButtonStub
    {
        private readonly Stream<bool> m_Stream;

        public ButtonStub(Stream<bool> stream)
        {
            m_Stream = stream;
        }

        public void Press()
        {
            m_Stream.OfferByValue(true);
        }

        public void Release()
        {
            m_Stream.OfferByValue(false);
        }
    }
}