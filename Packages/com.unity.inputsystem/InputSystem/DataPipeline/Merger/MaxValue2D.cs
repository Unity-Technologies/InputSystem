using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.Merger
{
    // x(N)+y(M)->z(N+M) conversion.
    public struct MaxValue2D : IPipelineStage
    {
        public StepFunction2D src1, src2, dst;

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

            var valuesSrc1X = datasetProxy.GetValuesX(src1);
            var valuesSrc1Y = datasetProxy.GetValuesY(src1);
            var valuesSrc2X = datasetProxy.GetValuesX(src2);
            var valuesSrc2Y = datasetProxy.GetValuesY(src2);
            var valuesDstX = datasetProxy.GetValuesX(dst);
            var valuesDstY = datasetProxy.GetValuesY(dst);

            var iSrc1 = 0;
            var iSrc2 = 0;
            var iDst = 0;

            var valueSrc1X = datasetProxy.GetPreviousValueX(src1);
            var valueSrc1Y = datasetProxy.GetPreviousValueY(src1);
            var valueSrc2X = datasetProxy.GetPreviousValueX(src2);
            var valueSrc2Y = datasetProxy.GetPreviousValueY(src2);
            var valueDstX = datasetProxy.GetPreviousValueX(dst);
            var valueDstY = datasetProxy.GetPreviousValueY(dst);
            
            var valueSrc1Mag = valueSrc1X * valueSrc1X + valueSrc1Y * valueSrc1Y;
            var valueSrc2Mag = valueSrc2X * valueSrc2X + valueSrc2Y * valueSrc2Y;
            var valueDstMag = valueDstX * valueDstX + valueDstY * valueDstY;

            while(iSrc1 < lengthSrc1 || iSrc2 < lengthSrc2)
            {
                ulong timestamp;

                // iterate over two timestamp axes and keep track of current values
                if (iSrc1 < lengthSrc1 && (iSrc2 >= lengthSrc2 || timestampsSrc1[iSrc1] <= timestampsSrc2[iSrc2]))
                {
                    timestamp = timestampsSrc1[iSrc1];
                    valueSrc1X = valuesSrc1X[iSrc1];
                    valueSrc1Y = valuesSrc1Y[iSrc1];
                    valueSrc1Mag = valueSrc1X * valueSrc1X + valueSrc1Y * valueSrc1Y;
                    iSrc1++;
                }
                else
                {
                    timestamp = timestampsSrc2[iSrc2];
                    valueSrc2X = valuesSrc2X[iSrc2];
                    valueSrc2Y = valuesSrc2Y[iSrc2];
                    valueSrc2Mag = valueSrc2X * valueSrc2X + valueSrc2Y * valueSrc2Y;
                    iSrc2++;
                }

                // choose maximum value between two
                var newValueMag = valueSrc1Mag > valueSrc2Mag ? valueSrc1Mag : valueSrc2Mag;

                // and record it if it's different from previous recorded max value 
                if (Math.Abs(newValueMag - valueDstMag) <= float.Epsilon) // TODO
                    continue;

                valueDstX = valueSrc1Mag > valueSrc2Mag ? valueSrc1X : valueSrc2X;
                valueDstY = valueSrc1Mag > valueSrc2Mag ? valueSrc1Y : valueSrc2Y;
                valueDstMag = valueDstX * valueDstX + valueDstY * valueDstY;

                timestampsDst[iDst] = timestamp;
                valuesDstX[iDst] = valueDstX;
                valuesDstY[iDst] = valueDstY;
                iDst++;
            }
            
            datasetProxy.ShrinkSizeTo(dst, iDst);
        }
    }
}