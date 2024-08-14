using System;

namespace UnityEngine.InputSystem.Experimental
{
    internal readonly unsafe struct UnsafeCallback : IEquatable<UnsafeCallback>
    {
        public readonly void* Function;
        public readonly void* Data;

        public UnsafeCallback(void* function, void* data)
        {
            Function = function;
            Data = data;
        }

        public bool Equals(UnsafeCallback other)
        {
            return Function == other.Function && Data == other.Data;
        }

        public override bool Equals(object obj)
        {
            return obj is UnsafeCallback other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(unchecked((int)(long)Function), unchecked((int)(long)Data));
        }

        public static bool operator ==(UnsafeCallback left, UnsafeCallback right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnsafeCallback left, UnsafeCallback right)
        {
            return !left.Equals(right);
        }
    }
}