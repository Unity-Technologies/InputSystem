using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.Merger
{
    // x(N)+y(M)->z(N+M) conversion.
    public struct MaxValue1D : IPipelineStage
    {
        public StepFunction1D src1, src2, dst;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(Dataset dataset)
        {
            dataset.MapNAndMToNPlusM(src1, src2, dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Dataset dataset)
        {
            var (lengthSrc1, lengthSrc2) = dataset.MapNAndMToNPlusM(src1, src2, dst);

            var timestampsSrc1 = dataset.GetTimestamps(src1);
            var timestampsSrc2 = dataset.GetTimestamps(src2);
            var timestampsDst = dataset.GetTimestamps(dst);

            var valuesSrc1 = dataset.GetValuesX(src1);
            var valuesSrc2 = dataset.GetValuesX(src2);
            var valuesDst = dataset.GetValuesX(dst);

            var iSrc1 = 0;
            var iSrc2 = 0;
            var iDst = 0;

            var valueSrc1 = dataset.GetPreviousValueX(src1);
            var valueSrc2 = dataset.GetPreviousValueX(src2);
            var valueDst = dataset.GetPreviousValueX(dst);

            while(iSrc1 < lengthSrc1 || iSrc2 < lengthSrc2)
            {
                ulong timestamp;

                // iterate over two timestamp axes and keep track of current values
                if (iSrc1 < lengthSrc1 && (iSrc2 >= lengthSrc2 || timestampsSrc1[iSrc1] <= timestampsSrc2[iSrc2]))
                {
                    timestamp = timestampsSrc1[iSrc1];
                    valueSrc1 = valuesSrc1[iSrc1];
                    iSrc1++;
                }
                else
                {
                    timestamp = timestampsSrc2[iSrc2];
                    valueSrc2 = valuesSrc2[iSrc2];
                    iSrc2++;
                }

                // choose maximum value between two
                var newValue = valueSrc1 > valueSrc2 ? valueSrc1 : valueSrc2;
                
                // and record it if it's different from previous recorded max value 
                if (Math.Abs(newValue - valueDst) <= float.Epsilon) // TODO
                    continue;

                timestampsDst[iDst] = timestamp;
                valuesDst[iDst] = newValue;
                valueDst = newValue;
                iDst++;
            }
            
            dataset.ShrinkSizeTo(dst, iDst);
        }
    }
}