using System;
using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a field of information.
    /// </summary>
    public readonly struct Field
    {
        public static readonly Field None = new();

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
    }
}
