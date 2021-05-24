namespace UnityEngine.InputSystem.DataPipeline.Demux.Dynamic
{
    public struct DemuxerDataField
    {
        public ulong maskA;
        public ulong maskB;
        public byte shiftA;
        public byte shiftB;
        public int pairIndex;
        public SourceDataType srcType;
        public DestinationDataType dstType;
        // destination if we counted from 0, needs device offset to be applied
        public int dstValueStepFunctionIndex;

        public int bitSize; // TODO remove me
        
        // TODO opaque fields
        
        // TODO add special mode for packed bitfields, like for keyboard
        // use popcount to figure out which bit is set
        
        public static DemuxerDataField From(
            int dstValueStepFunctionIndex,
            int bitOffset,
            int bitSize,
            int stateSizeInULongs,
            SourceDataType srcType,
            DestinationDataType dstType)
        {
            Debug.Assert((bitOffset + bitSize) / 64 <= stateSizeInULongs);
            
            var pairIndex = bitOffset / 64;
            
            // TODO
            //if (pairIndex + 1 == stateSizeInULongs) // move pointer one slot back so we read from upper value
            //    pairIndex--;

            bitOffset -= pairIndex * 64;


            var maskValue = (~0UL) >> (64 - bitSize);

            ulong maskA = 0;
            ulong maskB = 0;
            byte shiftA = 0;
            byte shiftB = 0;

            if (bitOffset == 0)
            {
                shiftA = 0;
                shiftB = 0;
                maskA = maskValue;
                maskB = 0; // special case, because we can only shift up to 63
            }
            else
            {
                shiftA = (byte) bitOffset;
                shiftB = (byte) (64 - bitOffset);
                maskA = maskValue << shiftA;
                maskB = maskValue >> shiftB;
            }
            
            return new DemuxerDataField
            {
                maskA = maskA,
                maskB = maskB,
                shiftA = shiftA,
                shiftB = shiftB,
                pairIndex = pairIndex,
                srcType = srcType,
                dstType = dstType,
                dstValueStepFunctionIndex = dstValueStepFunctionIndex,
                bitSize = bitSize
            };
        }
    }
}