using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.Merger
{
    // x(N)+y(M)->z(N+M) conversion.
    public struct MaxValue1D : IPipelineStage
    {
        public StepFunction1D src1, src2, dst;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(DatasetProxy datasetProxy)
        {
            datasetProxy.MapNAndMToNPlusM(src1, src2, dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(DatasetProxy datasetProxy)
        {
            var (lengthSrc1, lengthSrc2) = datasetProxy.MapNAndMToNPlusM(src1, src2, dst);

            var timestampsSrc1 = datasetProxy.GetTimestamps(src1);
            var timestampsSrc2 = datasetProxy.GetTimestamps(src2);
            var timestampsDst = datasetProxy.GetTimestamps(dst);

            var valuesSrc1 = datasetProxy.GetValuesX(src1);
            var valuesSrc2 = datasetProxy.GetValuesX(src2);
            var valuesDst = datasetProxy.GetValuesX(dst);

            var iSrc1 = 0;
            var iSrc2 = 0;
            var iDst = 0;

            var valueSrc1 = datasetProxy.GetPreviousValueX(src1);
            var valueSrc2 = datasetProxy.GetPreviousValueX(src2);
            var valueDst = datasetProxy.GetPreviousValueX(dst);

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
            
            datasetProxy.ShrinkSizeTo(dst, iDst);
        }
    }
}