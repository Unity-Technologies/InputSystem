using System;
using System.Runtime.InteropServices;
using Mono.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

namespace Tests.InputSystem.Experimental
{
    internal struct UnsafeArray<T> : IDisposable 
        where T : unmanaged
    {
        public unsafe T* data;
        public int capacity;
        public int length;
        private AllocatorManager.AllocatorHandle m_Allocator;

        public unsafe UnsafeArray(AllocatorManager.AllocatorHandle allocator)
        {
            data = null;
            capacity = 0;
            length = 0;
            m_Allocator = allocator;
        }
        
        public unsafe UnsafeArray(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            this.data = (T*)AllocatorManager.Allocate<T>(allocator, capacity);
            this.capacity = capacity;
            this.length = 0;
            m_Allocator = allocator;
        }
        
        public unsafe void Dispose()
        {
            if (data == null) return;
            AllocatorManager.Free(m_Allocator, data);
            data = null;
        }
        
        public unsafe ref T this[int key]
        {
            get => ref *(data + key);
        }

        public unsafe void EraseAt(int index)
        {
            var dst = data + index;
            UnsafeUtility.MemMove(dst, dst + 1, length - index);
            --length;
        }

        public unsafe void Resize(int newSize)
        {
            if (newSize > length)
            {
                Reserve(newSize);
                length = newSize;
            }
            else if (newSize < length)
            {
                length = newSize;
            }
        }
        
        public unsafe void Reserve(int newCapacity)
        {
            var newPtr = AllocatorManager.Allocate<T>(m_Allocator, newCapacity);
            if (data != null)
            {
                var newSize = Math.Min(length, newCapacity);
                UnsafeUtility.MemCpy(newPtr, data, newSize);
                AllocatorManager.Free(m_Allocator, data);
            }
            data = newPtr;
            capacity = newCapacity;
        }
    }
    
    // Key properties:
    // - The interval set contains no overlapping intervals.
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
                    m_Buffer.EraseAt(i);
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
    
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Interval : IEquatable<Interval>, IComparable<Interval>
    {
        public Interval(int lowerBound = 0, int upperBound = 0)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }

        public int lowerBound { get; }

        public int upperBound { get; }

        public int length => upperBound - lowerBound;

        public bool isEmpty => lowerBound >= upperBound;
        
        public bool Contains(int x)
        {
            return x >= lowerBound && x < upperBound;
        }

        public bool Contains(Interval other)
        {
            return lowerBound <= other.lowerBound && upperBound >= other.upperBound;
        }

        public Interval Intersection(Interval other)
        {
            return new Interval(
                Math.Max(lowerBound, other.lowerBound), 
                Math.Min(upperBound, other.upperBound));
        }

        internal unsafe int Add(Interval other, Interval* dst)
        {
            // Use the fact that (a + b) == a + (b \ a)
            var n = other.Subtract(this, dst);
            if (n < 0)
                *dst++ = other;

            return n;
        }

        internal unsafe int Subtract(Interval other, Interval* dst)
        {
            return Subtract(other, (int*)dst->lowerBound);
        }

        private unsafe int Subtract(Interval other, int* dst)
        {
            // Extract members in case destination is aliased with this interval
            var lower = lowerBound;
            var upper = upperBound;

            if (other.upperBound <= other.lowerBound)
                return 0;
            if (other.upperBound <= lower || other.lowerBound >= upper)
                return 0; // not intersecting

            var n = 1;
            dst[0] = lower;
            if (other.lowerBound > lower)
                dst[n++] = other.lowerBound;
            if (other.upperBound < upper)
                dst[n++] = other.upperBound;
            dst[n] = upper;
            return n;
        }
        
        #region IEquatable<Interval>
        
        public bool Equals(Interval other)
        {
            return lowerBound == other.lowerBound && upperBound == other.upperBound;
        }

        public override bool Equals(object obj)
        {
            return obj is Interval other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(lowerBound, upperBound);
        }

        public static bool operator ==(Interval left, Interval right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Interval left, Interval right)
        {
            return !left.Equals(right);
        }
        
        #endregion

        public override string ToString()
        {
            return $"{nameof(Interval)}{{{nameof(lowerBound)}: {lowerBound}, {nameof(upperBound)}: {upperBound}, {nameof(length)}: {length}}}";
        }

        #region IComparable<Interval>
        
        public int CompareTo(Interval other)
        {
            var lowerBoundComparison = lowerBound.CompareTo(other.lowerBound);
            return lowerBoundComparison != 0 ? lowerBoundComparison : upperBound.CompareTo(other.upperBound);
        }
        
        #endregion
    }
    
    [Category("Experimental")]
    internal sealed class IntervalTests
    {
        [Test]
        [TestCase(0, 0, true)]
        [TestCase(0, 1, false)]
        [TestCase(1, 1, true)]
        [TestCase(-1, -1, true)]
        [TestCase(int.MinValue, int.MaxValue, false)]
        [TestCase(int.MaxValue, int.MinValue, true)]
        public void isEmpty_ShouldReturnTrue_OnlyIfIntervalHasPositiveLength(int lower, int upper, bool expected)
        {
            Assert.That(new Interval(lower, upper).isEmpty, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(0, 0, 0)]
        [TestCase(0, 1, 1)]
        [TestCase(1, 1, 0)]
        [TestCase(-1, -1, 0)]
        //[TestCase(int.MinValue, int.MaxValue, 0)]
        //[TestCase(int.MaxValue, int.MinValue, 0)]
        public void length_ShouldReturnRange(int lower, int upper, int expected)
        {
            Assert.That(new Interval(lower, upper).length, Is.EqualTo(expected));
        }

        [TestCase(0, 0, 0, false)]
        [TestCase(0, 1, 0, true)]
        [TestCase(0, 1, 1, false)]
        [TestCase(1, 1, 1, false)]
        [TestCase(-1, -1, -1, false)]
        [TestCase(-1, -1, 0, false)]
        public void contains_ShouldReturnTrue_IfIntervalContainsPoint(int lower, int upper, int p, bool expected)
        {
            Assert.That(new Interval(lower, upper).Contains(p), Is.EqualTo(expected));
        }
    }
}