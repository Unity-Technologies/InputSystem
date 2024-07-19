using System;
using System.Collections.Generic;
using System.Globalization;

namespace UnityEngine.InputSystem.Experimental
{
    public interface ITimestamped<T>
    {
        public long timestamp { get; }
        public T value { get; }
    }

    public readonly struct Timestamped<T> : ITimestamped<T>, IEquatable<Timestamped<T>>
    {
        public Timestamped(T value, long timestamp)
        {
            this.value = value;
            this.timestamp = timestamp;
        }

        public long timestamp { get; }
        public T value { get; }

        public bool Equals(Timestamped<T> other)
        {
            return timestamp == other.timestamp && EqualityComparer<T>.Default.Equals(value, other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is Timestamped<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(timestamp, value);
        }

        public static bool operator ==(Timestamped<T> left, Timestamped<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Timestamped<T> left, Timestamped<T> right)
        {
            return !left.Equals(right);
        }
        
        public override string ToString() => string.Format(CultureInfo.CurrentCulture, "{0}@{1}", value, timestamp);
    }
}