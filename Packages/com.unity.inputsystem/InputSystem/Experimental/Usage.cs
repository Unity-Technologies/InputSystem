using System;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a usage.1
    /// </summary>
    [Serializable]
    public struct Usage : IEquatable<Usage>
    {
        public static readonly Usage Invalid = new(0); // TODO Delete since default?

        public uint value;

        public Usage(uint value) { this.value = value; }
        public Usage(ushort page, ushort id)
            : this((uint)page << 16 | id)
        {  }
        public ushort page => (ushort)(value >> 16);
        public ushort id => (ushort)value;
        public static implicit operator bool(Usage usage) => usage != Invalid;
        public static explicit operator Usage(uint value) => new Usage(value);
        public static explicit operator uint(Usage usage) => usage.value;
        public bool Equals(Usage other) => value == other.value;
        public override bool Equals(object obj) => obj is Usage other && Equals(other);
        public override int GetHashCode() => value.GetHashCode();
        public static bool operator==(Usage lhs, Usage rhs) => lhs.Equals(rhs);
        public static bool operator!=(Usage lhs, Usage rhs) => !(lhs == rhs);

        public override string ToString()
        {
            return $"0x{value:x8}";
        }
    }
}
