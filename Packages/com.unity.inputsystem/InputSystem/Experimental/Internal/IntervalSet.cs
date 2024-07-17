using System;
using Unity.Collections;

namespace Tests.InputSystem.Experimental
{
    internal struct IntervalSet
    {
        private UnsafeArray<Interval> m_Buffer;

        public unsafe IntervalSet(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Buffer = new UnsafeArray<Interval>(capacity, allocator);
        }

        public int count => m_Buffer.length;
        
        public void Add(Interval interval)
        {
            
        }

        public unsafe void Subtract(Interval interval)
        {
            // Return immediately if set is empty.
            if (m_Buffer.length == 0)
                return; 

            // Ensure capacity is sufficient to utilize tail of container as temporary buffer.
            var remainingCapacity = m_Buffer.capacity - m_Buffer.length;
            if (remainingCapacity < 2)
                m_Buffer.Reserve(Math.Max(m_Buffer.capacity + 4, m_Buffer.length * 2));

            // Subtract interval from this set by piecewise subtraction with intersecting elements.
            var n = m_Buffer.length;
            for (var i = 0; i < n;)
            {
                int result = m_Buffer[i].Subtract(interval, m_Buffer.data + m_Buffer.length);
                if (result == 0)
                {
                    // Remove element since eliminated when subtracting interval
                    m_Buffer.RemoveAt(i);
                    --n;
                    continue;
                }
                
                // If greater than zero elements have already been copied to buffer
                if (result > 0)
                    m_Buffer.Resize(m_Buffer.length + result);
                
                ++i;
            }
        }

        private unsafe static int Subtract(Interval a, Interval b, int* dst)
        {
            if (b.upperBound <= a.lowerBound || b.lowerBound >= a.upperBound)
                return 0; // not intersecting

            var n = 1;
            dst[0] = a.lowerBound;
            if (b.lowerBound > a.lowerBound)
                dst[n++] = b.lowerBound; // interval0.upper
            if (b.upperBound < a.upperBound)
                dst[n++] = b.upperBound; // interval1.lower or interval0.upper
            dst[n] = a.upperBound;
            return n;
        }
    }
}