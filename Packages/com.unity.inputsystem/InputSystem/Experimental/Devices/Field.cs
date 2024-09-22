using System;
using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider if we want readonly (Not serializable by Unity)
    /// <summary>
    /// Represents a data field of information.
    /// </summary>
    [Serializable]
    public readonly struct Field : IEquatable<Field>
    {
        public readonly uint ByteOffset;
        public readonly ushort BitOffset;
        public readonly ushort BitLength;
        
        private Field(uint byteOffset = 0, ushort bitOffset = 0, ushort bitLength = ushort.MaxValue)
        {
            ByteOffset = byteOffset;
            BitOffset = bitOffset;
            BitLength = bitLength;
        }
        
        public static Field Offset(int byteOffset)
        {
            return new Field(byteOffset: 0);
        }

        public static Field Bit(uint fieldByteOffset, ushort bitIndex)
        {
            return new Field(byteOffset: fieldByteOffset, bitOffset: bitIndex, bitLength: 1);
        }

        public static Field Bit(uint bitIndex)
        {
            return new Field(bitIndex >> 3, (ushort)(bitIndex & 7), 1);
        }

        public bool Equals(Field other)
        {
            return ByteOffset == other.ByteOffset && BitOffset == other.BitOffset && BitLength == other.BitLength;
        }

        public override bool Equals(object obj)
        {
            return obj is Field other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ByteOffset, BitOffset, BitLength);
        }

        public static bool operator ==(Field left, Field right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Field left, Field right)
        {
            return !left.Equals(right);
        }
    }
}
