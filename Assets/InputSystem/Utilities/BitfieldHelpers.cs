using System;

namespace ISX
{
    internal static class BitfieldHelpers
    {
        public static uint ComputeFollowingByteOffset(uint byteOffset, uint sizeInBits)
        {
            return (uint)(byteOffset + sizeInBits / 8 + ((sizeInBits % 8) > 0 ? 1 : 0));
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
    }
}
