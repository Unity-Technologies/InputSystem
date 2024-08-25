using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a usage.
    /// </summary>
    public readonly struct Usage : IEquatable<Usage>
    {
        public static readonly Usage Invalid = new(0);

        public readonly uint Value;

        public Usage(uint value) { Value = value; }
        public Usage(ushort page, ushort id)
            : this((uint)page << 16 | id)
        {  }
        public ushort page => (ushort)(Value >> 16);
        public ushort id => (ushort)Value;
        public static implicit operator bool(Usage usage) => usage != Invalid;
        public static explicit operator Usage(uint value) => new Usage(value);
        public static explicit operator uint(Usage usage) => usage.Value;
        public bool Equals(Usage other) => Value == other.Value;
        public override bool Equals(object obj) => obj is Usage other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator==(Usage lhs, Usage rhs) => lhs.Equals(rhs);
        public static bool operator!=(Usage lhs, Usage rhs) => !(lhs == rhs);

        public override string ToString()
        {
            return $"0x{Value:x8}";
        }
    }
}
