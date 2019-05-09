using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Struct replacement for System.Collections.Bitfield.
    /// </summary>
    /// <remarks>
    /// We don't want the extra heap object just for keeping the header
    /// state of the bitfield. This struct directly embeds the header
    /// into the owner. Also doesn't allocate any array while length is
    /// less than or equal to 64 bits.
    /// </remarks>
    internal struct DynamicBitfield
    {
        public InlinedArray<ulong> array;
        public int length;

        public void SetLength(int newLength)
        {
            // Don't touch array size if we don't have to. We're fine having a
            // larger array to work with if it's already in place.
            var ulongCount = BitCountToULongCount(newLength);
            if (array.length < ulongCount)
                array.SetLength(ulongCount);

            length = newLength;
        }

        public void SetBit(int bitIndex)
        {
            Debug.Assert(bitIndex >= 0);
            Debug.Assert(bitIndex < length);

            array[bitIndex / 64] |= (ulong)1 << (bitIndex % 64);
        }

        public bool TestBit(int bitIndex)
        {
            Debug.Assert(bitIndex >= 0);
            Debug.Assert(bitIndex < length);

            return (array[bitIndex / 64] & ((ulong)1 << (bitIndex % 64))) != 0;
        }

        public void ClearBit(int bitIndex)
        {
            Debug.Assert(bitIndex >= 0);
            Debug.Assert(bitIndex < length);

            array[bitIndex / 64] &= ~((ulong)1 << (bitIndex % 64));
        }

        private static int BitCountToULongCount(int bitCount)
        {
            return (bitCount + 63) / 64;
        }
    }
}
