using System;

namespace UnityEngine.Experimental.Input.Utilities
{
    internal static class MemoryHelpers
    {
        public static uint ComputeFollowingByteOffset(uint byteOffset, uint sizeInBits)
        {
            return (uint)(byteOffset + sizeInBits / 8 + ((sizeInBits % 8) > 0 ? 1 : 0));
        }

        public static bool MemoryOverlapsBitRegion(uint byteOffset, uint bitOffset, uint sizeInBits, uint memoryOffset,
            uint memorySizeInBytes)
        {
            if (sizeInBits % 8 == 0 && bitOffset == 0)
            {
                // Simple byte aligned case.
                return byteOffset + sizeInBits / 8 > memoryOffset && memoryOffset + memorySizeInBytes > byteOffset;
            }

            // Bit aligned case.
            if (memoryOffset > byteOffset)
            {
                return bitOffset + sizeInBits > ((ulong)(memoryOffset - byteOffset)) * 8;
            }
            return ((ulong)(memorySizeInBytes * 8)) > (((ulong)(byteOffset - memoryOffset)) * 8 + bitOffset);
        }

        public static unsafe void WriteSingleBit(IntPtr ptr, uint bitOffset, bool value)
        {
            if (bitOffset < 8)
            {
                if (value)
                    *((byte*)ptr) |= (byte)(1 << (int)bitOffset);
                else
                    *((byte*)ptr) &= (byte)~(1 << (int)bitOffset);
            }
            else if (bitOffset < 32)
            {
                if (value)
                    *((int*)ptr) |= 1 << (int)bitOffset;
                else
                    *((int*)ptr) &= ~(1 << (int)bitOffset);
            }
            else
            {
                var byteOffset = bitOffset / 8;
                bitOffset = bitOffset % 8;

                if (value)
                    *((byte*)ptr + byteOffset) |= (byte)(1 << (int)bitOffset);
                else
                    *((byte*)ptr + byteOffset) &= (byte)~(1 << (int)bitOffset);
            }
        }

        public static unsafe bool ReadSingleBit(IntPtr ptr, uint bitOffset)
        {
            ////TODO: currently this is not actually enforced...
            // The layout code makes sure that bitfields are either 8bit or multiples
            // of 32bits. So we always safely read either a byte or int. Handling
            // the 8bit and 32bit case directly will lead to nicely aligned memory
            // accesses if the state has been laid out that way.

            int bits;

            if (bitOffset < 8)
            {
                bits = *((byte*)ptr);
            }
            else if (bitOffset < 32)
            {
                bits = *((int*)ptr);
            }
            else
            {
                // Long bitfield. Compute an offset to the byte we need and fetch
                // only that byte. Adjust the bit offset to be for that byte.
                // On this path, we may end up doing memory accesses that the CPU
                // doesn't like much.

                var byteOffset = bitOffset / 8;
                bitOffset = bitOffset % 8;

                bits = *((byte*)ptr + byteOffset);
            }

            return (bits & (1 << (int)bitOffset)) != 0;
        }

        public static unsafe int ReadMultipleBits(IntPtr ptr, uint bitOffset, uint bitCount)
        {
            if (bitCount >= sizeof(int) * 8)
                throw new ArgumentException("Trying to read more than 32 bits as int", "bitCount");

            // Bits out of byte.
            if (bitOffset + bitCount <= 8)
            {
                var value = *(byte*)ptr;
                value >>= (int)bitOffset;
                var mask = 0xFF >> (8 - (int)bitCount);
                return value & mask;
            }

            // Bits out of short.
            if (bitOffset + bitCount <= 16)
            {
                var value = *(ushort*)ptr;
                value >>= (int)bitOffset;
                var mask = 0xFFFF >> (16 - (int)bitCount);
                return value & mask;
            }

            // Bits out of int.
            if (bitOffset + bitCount <= 32)
            {
                var value = *(uint*)ptr;
                value >>= (int)bitOffset;
                var mask = 0xFFFFFFFF >> (16 - (int)bitCount);
                return (int)(value & mask);
            }

            throw new NotImplementedException("Reading unaligned int");
        }
    }
}
