using System;
using System.Runtime.InteropServices;

namespace Tests.InputSystem.Experimental
{
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
}