using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Tests.InputSystem.Experimental
{
    internal struct IntervalSet
    {
        private NativeList<Interval> m_Buffer;

        public unsafe IntervalSet(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Buffer = new NativeList<Interval>(capacity, allocator);
        }

        public int count => m_Buffer.Length;
        
        public void Add(Interval interval)
        {
            
        }

        public unsafe void Subtract(Interval interval)
        {
            // Return immediately if set is empty.
            var n = m_Buffer.Length;
            if (n == 0)
                return; 

            // Ensure capacity is sufficient to utilize tail of container as temporary buffer.
            var remainder = stackalloc Interval[2];

            // Subtract interval from this set by piecewise subtraction with intersecting elements.
            for (var i = 0; i < n;)
            {
                var result = Subtract(m_Buffer[i], interval, (int*)remainder);
                switch (result)
                {
                    case 0:
                        // Remove element since eliminated when subtracting interval
                        m_Buffer.RemoveAt(i);
                        --n;
                        continue;

                    case > 0:
                        // If greater than zero elements we copy remaining elements to buffer
                        m_Buffer.AddRange(remainder, 2);
                        break;
                }

                ++i;
            }
        }

        private static unsafe int Subtract(Interval a, Interval b, int* dst)
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