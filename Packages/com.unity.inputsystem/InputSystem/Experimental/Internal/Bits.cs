using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.Experimental.Internal
{
    public static class Bits
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetBit(uint bits, int bitIndex, bool value) =>
            value ? SetBit(bits, bitIndex) : ClearBit(bits, bitIndex);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetBit(uint value, int bitIndex) => value | (1U << (bitIndex & 31));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBit(uint value, int bitIndex) => (value & bitIndex) != 0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ClearBit(uint value, int bitIndex) => (value & ~(1U << (bitIndex & 31)));
        
    }
}