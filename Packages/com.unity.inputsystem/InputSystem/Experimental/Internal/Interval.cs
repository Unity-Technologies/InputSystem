using System;
using System.Runtime.InteropServices;

namespace Tests.InputSystem.Experimental
{
    /// <summary>
    /// Represents a numeric interval.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Interval : IEquatable<Interval>, IComparable<Interval>
    {
        /// <summary>
        /// Constructs a new interval from the given bounds.
        /// </summary>
        /// <param name="lowerBound">The lower bound of the interval (inclusive).</param>
        /// <param name="upperBound">The upper bound of the interval (exclusive).</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="lowerBound"/> is greater than
        /// <paramref name="upperBound"/>.</exception>
        public Interval(int lowerBound = 0, int upperBound = 0)
        {
            if (lowerBound > upperBound)
                throw new ArgumentOutOfRangeException($"'{nameof(lowerBound)}' must be less or equal to '{nameof(upperBound)}'");
            
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }

        /// <summary>
        /// Returns the lower bound of the interval (inclusive).
        /// </summary>
        public int lowerBound { get; }

        /// <summary>
        /// Returns the upper bound of the interval (exclusive).
        /// </summary>
        public int upperBound { get; }

        /// <summary>
        /// Returns the length of the interval.
        /// </summary>
        public int length => upperBound - lowerBound;

        /// <summary>
        /// Returns whether the interval is empty.
        /// </summary>
        public bool isEmpty => length == 0;
        
        /// <summary>
        /// Returns whether this instance contains <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to be tested.</param>
        /// <returns></returns>
        public bool Contains(int value) => value >= lowerBound && value < upperBound;

        /// <summary>
        /// Returns whether this instance contains sub-interval <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The interval to be tested.</param>
        /// <returns><c>true</c> if this interval contains <paramref name="other"/>, else <c>false</c>.</returns>
        public bool Contains(Interval other) => lowerBound <= other.lowerBound && upperBound >= other.upperBound;

        /// <summary>
        /// Returns the intersection of this instance and <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other interval.</param>
        /// <returns>An interval representing an intersection of this interval and <paramref name="other"/>.</returns>
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