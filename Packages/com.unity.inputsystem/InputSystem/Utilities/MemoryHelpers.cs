using System;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Input.Utilities
{
    internal static unsafe class MemoryHelpers
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
            return memorySizeInBytes * 8 > ((ulong)(byteOffset - memoryOffset)) * 8 + bitOffset;
        }

        public static void WriteSingleBit(IntPtr ptr, uint bitOffset, bool value)
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

        public static bool ReadSingleBit(IntPtr ptr, uint bitOffset)
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

        /// <summary>
        /// Compare two memory regions that may be offset by a bit count and have a length expressed
        /// in bits.
        /// </summary>
        /// <param name="ptr1">Pointer to start of first memory region.</param>
        /// <param name="ptr2">Pointer to start of second memory region.</param>
        /// <param name="bitOffset">Offset in bits from each of the pointers to the start of the memory region to compare.</param>
        /// <param name="bitCount">Number of bits to compare in the memory region.</param>
        /// <returns>True if the two memory regions are identical, false otherwise.</returns>
        public static bool MemCmpBitRegion(void* ptr1, void* ptr2, uint bitOffset, uint bitCount)
        {
            var bytePtr1 = (byte*)ptr1;
            var bytePtr2 = (byte*)ptr2;

            // If we're offset by more than a byte, adjust our pointers.
            if (bitOffset > 8)
            {
                var skipBytes = bitOffset / 8;
                bytePtr1 += skipBytes;
                bytePtr2 += skipBytes;
                bitOffset %= 8;
            }

            // Compare unaligned prefix, if any.
            if (bitOffset > 0)
            {
                // If the total length of the memory region is less than a byte, we need
                // to mask out parts of the bits we're reading.
                var mask = 0xFF;
                if (bitCount + bitOffset < 8)
                {
                    mask = 0xFF >> (int)(8 - (bitCount + bitOffset));
                }

                var byte1 = (*bytePtr1 >> (int)bitOffset) & mask;
                var byte2 = (*bytePtr2 >> (int)bitOffset) & mask;

                if (byte1 != byte2)
                    return false;

                ++bytePtr1;
                ++bytePtr2;
                bitOffset = 0;

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
                if (UnsafeUtility.MemCmp(bytePtr1, bytePtr2, byteCount) != 0)
                    return false;
            }

            // Compare unaligned suffix, if any.
            var remainingBitCount = bitCount % 8;
            if (remainingBitCount > 0)
            {
                bytePtr1 += byteCount;
                bytePtr2 += byteCount;

                // We want the lowest remaining bits.
                var mask = 0xFF >> (int)(8 - remainingBitCount);

                var byte1 = *bytePtr1 & mask;
                var byte2 = *bytePtr2 & mask;

                if (byte1 != byte2)
                    return false;
            }

            return true;
        }

        public static int ReadIntFromMultipleBits(IntPtr ptr, uint bitOffset, uint bitCount)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException("ptr");
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
                var mask = 0xFFFFFFFF >> (32 - (int)bitCount);
                return (int)(value & mask);
            }

            throw new NotImplementedException("Reading int straddling int boundary");
        }

        public static void WriteIntFromMultipleBits(IntPtr ptr, uint bitOffset, uint bitCount, int value)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException("ptr");
            if (bitCount >= sizeof(int) * 8)
                throw new ArgumentException("Trying to write more than 32 bits as int", "bitCount");

            // Bits out of byte.
            if (bitOffset + bitCount <= 8)
            {
                var byteValue = (byte)value;
                byteValue >>= (int)bitOffset;
                var mask = 0xFF >> (8 - (int)bitCount);
                *((byte*)ptr) |= (byte)(byteValue & mask);
                return;
            }

            // Bits out of short.
            if (bitOffset + bitCount <= 16)
            {
                var shortValue = (ushort)value;
                shortValue >>= (int)bitOffset;
                var mask = 0xFFFF >> (16 - (int)bitCount);
                *((ushort*)ptr) |= (ushort)(shortValue & mask);
                return;
            }

            // Bits out of int.
            if (bitOffset + bitCount <= 32)
            {
                var intValue = (uint)value;
                intValue >>= (int)bitOffset;
                var mask = 0xFFFFFFFF >> (32 - (int)bitCount);
                *((uint*)ptr) |= intValue & mask;
                return;
            }

            throw new NotImplementedException("Writing int straddling int boundary");
        }

        public static void SetBitsInBuffer(IntPtr filterBuffer, InputControl control, bool value)
        {
            SetBitsInBuffer(filterBuffer, control.stateBlock.byteOffset, control.stateBlock.sizeInBits, value);
        }

        public static void SetBitsInBuffer(IntPtr filterBuffer, uint byteOffset, uint sizeInBits, bool value)
        {
            if (filterBuffer == IntPtr.Zero)
                throw new ArgumentException("A buffer must be provided to apply the bitmask on", "filterBuffer");

            var sizeRemaining = sizeInBits;

            var filterIter = (uint*)((filterBuffer.ToInt64() + (Int64)byteOffset));
            while (sizeRemaining >= 32)
            {
                *filterIter = value ? 0xFFFFFFFF : 0;
                filterIter++;
                sizeRemaining -= 32;
            }

            var mask = (uint)((1 << (int)sizeRemaining) - 1);
            if (value)
            {
                *filterIter |= mask;
            }
            else
            {
                *filterIter &= ~mask;
            }
        }

        public static bool HasAnyNonZeroBitsAfterMaskingWithBuffer(IntPtr eventBuffer, IntPtr maskPtr, uint offsetBytes, uint sizeInBits)
        {
            if (eventBuffer == IntPtr.Zero || maskPtr == IntPtr.Zero)
                return false;

            var sizeRemaining = sizeInBits;
            var eventIter = (uint*)eventBuffer.ToPointer();
            var maskIter = (uint*)(new IntPtr(maskPtr.ToInt64() + (Int64)offsetBytes).ToPointer());

            while (sizeRemaining >= 32)
            {
                if ((*eventIter & *maskIter) != 0)
                    return true;

                eventIter++;
                maskIter++;

                sizeRemaining -= 32;
            }

            //Find the remaining bytes to check
            // Mask it in the state iterator and noise
            var remainingState = *eventIter;
            var remainingMask = *maskIter;

            var mask = ((1 >> (int)sizeRemaining) - 1);
            if ((remainingState & (remainingMask & mask)) != 0)
                return true;

            return false;
        }
    }
}
