using System;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Utilities
{
    internal static unsafe class MemoryHelpers
    {
        public static uint ComputeFollowingByteOffset(uint byteOffset, uint sizeInBits)
        {
            return (uint)(byteOffset + sizeInBits / 8 + (sizeInBits % 8 > 0 ? 1 : 0));
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
            return memorySizeInBytes * 8 > (ulong)(byteOffset - memoryOffset) * 8 + bitOffset;
        }

        public static void WriteSingleBit(void* ptr, uint bitOffset, bool value)
        {
            if (bitOffset < 8)
            {
                if (value)
                    *(byte*)ptr |= (byte)(1 << (int)bitOffset);
                else
                    *(byte*)ptr &= (byte)~(1 << (int)bitOffset);
            }
            else if (bitOffset < 32)
            {
                if (value)
                    *(int*)ptr |= 1 << (int)bitOffset;
                else
                    *(int*)ptr &= ~(1 << (int)bitOffset);
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

        public static bool ReadSingleBit(void* ptr, uint bitOffset)
        {
            ////TODO: currently this is not actually enforced...
            // The layout code makes sure that bitfields are either 8bit or multiples
            // of 32bits. So we always safely read either a byte or int. Handling
            // the 8bit and 32bit case directly will lead to nicely aligned memory
            // accesses if the state has been laid out that way.

            int bits;

            if (bitOffset < 8)
            {
                bits = *(byte*)ptr;
            }
            else if (bitOffset < 32)
            {
                bits = *(int*)ptr;
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

        /// <summary>
        /// Compare two memory regions that may be offset by a bit count and have a length expressed
        /// in bits.
        /// </summary>
        /// <param name="ptr1">Pointer to start of first memory region.</param>
        /// <param name="ptr2">Pointer to start of second memory region.</param>
        /// <param name="bitOffset">Offset in bits from each of the pointers to the start of the memory region to compare.</param>
        /// <param name="bitCount">Number of bits to compare in the memory region.</param>
        /// <param name="mask">If not null, ignore any bits set in the mask. This allows comparing two memory regions while
        /// ignoring specific bits.</param>
        /// <returns>True if the two memory regions are identical, false otherwise.</returns>
        public static bool MemCmpBitRegion(void* ptr1, void* ptr2, uint bitOffset, uint bitCount, void* mask = null)
        {
            var bytePtr1 = (byte*)ptr1;
            var bytePtr2 = (byte*)ptr2;
            var maskPtr = (byte*)mask;

            // If we're offset by more than a byte, adjust our pointers.
            if (bitOffset >= 8)
            {
                var skipBytes = bitOffset / 8;
                bytePtr1 += skipBytes;
                bytePtr2 += skipBytes;
                if (maskPtr != null)
                    maskPtr += skipBytes;
                bitOffset %= 8;
            }

            // Compare unaligned prefix, if any.
            if (bitOffset > 0)
            {
                // If the total length of the memory region is less than a byte, we need
                // to mask out parts of the bits we're reading.
                var byteMask = 0xFF << (int)bitOffset;
                if (bitCount + bitOffset < 8)
                {
                    byteMask &= 0xFF >> (int)(8 - (bitCount + bitOffset));
                }

                if (maskPtr != null)
                {
                    byteMask &= *maskPtr;
                    ++maskPtr;
                }

                var byte1 = *bytePtr1 & byteMask;
                var byte2 = *bytePtr2 & byteMask;

                if (byte1 != byte2)
                    return false;

                ++bytePtr1;
                ++bytePtr2;

                // If the total length of the memory region is equal or less than a byte,
                // we're done.
                if (bitCount + bitOffset <= 8)
                    return true;

                bitCount -= 8 - bitOffset;
            }

            // Compare contiguous bytes in-between, if any.
            var byteCount = bitCount / 8;
            if (byteCount >= 1)
            {
                if (maskPtr != null)
                {
                    ////REVIEW: could go int by int here for as long as we can
                    // Have to go byte-by-byte in order to apply the masking.
                    for (var i = 0; i < byteCount; ++i)
                    {
                        var byte1 = bytePtr1[i];
                        var byte2 = bytePtr2[i];
                        var byteMask = maskPtr[i];

                        if ((byte1 & byteMask) != (byte2 & byteMask))
                            return false;
                    }
                }
                else
                {
                    if (UnsafeUtility.MemCmp(bytePtr1, bytePtr2, byteCount) != 0)
                        return false;
                }
            }

            // Compare unaligned suffix, if any.
            var remainingBitCount = bitCount % 8;
            if (remainingBitCount > 0)
            {
                bytePtr1 += byteCount;
                bytePtr2 += byteCount;

                // We want the lowest remaining bits.
                var byteMask = 0xFF >> (int)(8 - remainingBitCount);

                if (maskPtr != null)
                {
                    maskPtr += byteCount;
                    byteMask &= *maskPtr;
                }

                var byte1 = *bytePtr1 & byteMask;
                var byte2 = *bytePtr2 & byteMask;

                if (byte1 != byte2)
                    return false;
            }

            return true;
        }

        public static int ReadIntFromMultipleBits(void* ptr, uint bitOffset, uint bitCount)
        {
            if (ptr == null)
                throw new ArgumentNullException("ptr");
            if (bitCount >= sizeof(int) * 8)
                throw new ArgumentException("Trying to read more than 32 bits as int", "bitCount");

            //Shift the pointer up on larger bitmasks and retry
            if (bitOffset > 32)
            {
                int newBitOffset = (int)bitOffset % 32;
                int intOffset = ((int)bitOffset - newBitOffset) / 32;
                ptr = (byte*)ptr + (intOffset * 4);
                bitOffset = (uint)newBitOffset;
            }

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
                var mask = 0xFFFFFFFF >> (32 - (int)bitCount);
                return (int)(value & mask);
            }

            throw new NotImplementedException("Reading int straddling int boundary");
        }

        public static void WriteIntFromMultipleBits(void* ptr, uint bitOffset, uint bitCount, int value)
        {
            if (ptr == null)
                throw new ArgumentNullException("ptr");
            if (bitCount >= sizeof(int) * 8)
                throw new ArgumentException("Trying to write more than 32 bits as int", "bitCount");

            // Bits out of byte.
            if (bitOffset + bitCount <= 8)
            {
                var byteValue = (byte)value;
                byteValue >>= (int)bitOffset;
                var mask = 0xFF >> (8 - (int)bitCount);
                *(byte*)ptr |= (byte)(byteValue & mask);
                return;
            }

            // Bits out of short.
            if (bitOffset + bitCount <= 16)
            {
                var shortValue = (ushort)value;
                shortValue >>= (int)bitOffset;
                var mask = 0xFFFF >> (16 - (int)bitCount);
                *(ushort*)ptr |= (ushort)(shortValue & mask);
                return;
            }

            // Bits out of int.
            if (bitOffset + bitCount <= 32)
            {
                var intValue = (uint)value;
                intValue >>= (int)bitOffset;
                var mask = 0xFFFFFFFF >> (32 - (int)bitCount);
                *(uint*)ptr |= intValue & mask;
                return;
            }

            throw new NotImplementedException("Writing int straddling int boundary");
        }

        public static void SetBitsInBuffer(void* buffer, int byteOffset, int bitOffset, int sizeInBits, bool value)
        {
            if (buffer == null)
                throw new ArgumentException("A buffer must be provided to apply the bitmask on", "buffer");
            if (sizeInBits < 0)
                throw new ArgumentException("Negative sizeInBits", "sizeInBits");
            if (bitOffset < 0)
                throw new ArgumentException("Negative bitOffset", "bitOffset");
            if (byteOffset < 0)
                throw new ArgumentException("Negative byteOffset", "byteOffset");

            // If we're offset by more than a byte, adjust our pointers.
            if (bitOffset >= 8)
            {
                var skipBytes = bitOffset / 8;
                byteOffset += skipBytes;
                bitOffset %= 8;
            }

            var bytePos = (byte*)buffer + byteOffset;
            var sizeRemainingInBits = sizeInBits;

            // Handle first byte separately if unaligned to byte boundary.
            if (bitOffset != 0)
            {
                var mask = 0xFF << bitOffset;
                if (sizeRemainingInBits + bitOffset < 8)
                {
                    mask &= 0xFF >> (8 - (sizeRemainingInBits + bitOffset));
                }

                if (value)
                    *bytePos |= (byte)mask;
                else
                    *bytePos &= (byte)~mask;
                ++bytePos;
                sizeRemainingInBits -= 8 - bitOffset;
            }

            // Handle full bytes in-between.
            while (sizeRemainingInBits >= 8)
            {
                *bytePos = value ? (byte)0xFF : (byte)0;
                ++bytePos;
                sizeRemainingInBits -= 8;
            }

            // Handle unaligned trailing byte, if present.
            if (sizeRemainingInBits > 0)
            {
                var mask = (byte)(0xFF >> 8 - sizeRemainingInBits);
                if (value)
                    *bytePos |= mask;
                else
                    *bytePos &= (byte)~mask;
            }

            Debug.Assert(bytePos <= (byte*)buffer +
                ComputeFollowingByteOffset((uint)byteOffset, (uint)bitOffset + (uint)sizeInBits));
        }

        public static void Swap<TValue>(ref TValue a, ref TValue b)
        {
            var temp = a;
            a = b;
            b = temp;
        }
    }
}
