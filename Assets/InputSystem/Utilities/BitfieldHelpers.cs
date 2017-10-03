namespace ISX
{
    internal static class BitfieldHelpers
    {
        public static uint ComputeFollowingByteOffset(uint byteOffset, uint sizeInBits)
        {
            return (uint)(byteOffset + sizeInBits / 8 + ((sizeInBits % 8) > 0 ? 1 : 0));
        }
    }
}
