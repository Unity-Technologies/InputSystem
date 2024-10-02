using System;

namespace UnityEngine.InputSystem.Experimental
{
    public readonly struct OutputBindingTarget<T> where T : struct
    {
        private readonly Usage m_Usage;

        public OutputBindingTarget(Usage usage)
        {
            m_Usage = usage;
        }

        public void Offer(T value)
        {
            Offer(ref value);
        }

        public void Offer(ref T value)
        {
            throw new NotImplementedException("TODO Store data in stream if bound (has an associated stream)");
        }
    }
}