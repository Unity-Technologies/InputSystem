using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.Merger
{
    // x(N)+y(M)->z(N+M) conversion.
    public struct MaxValue2D : IPipelineStage
    {
        public StepFunction2D src1, src2, dst;

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

            var valuesSrc1X = dataset.GetValuesX(src1);
            var valuesSrc1Y = dataset.GetValuesY(src1);
            var valuesSrc2X = dataset.GetValuesX(src2);
            var valuesSrc2Y = dataset.GetValuesY(src2);
            var valuesDstX = dataset.GetValuesX(dst);
            var valuesDstY = dataset.GetValuesY(dst);

            var iSrc1 = 0;
            var iSrc2 = 0;
            var iDst = 0;

            var valueSrc1X = dataset.GetPreviousValueX(src1);
            var valueSrc1Y = dataset.GetPreviousValueY(src1);
            var valueSrc2X = dataset.GetPreviousValueX(src2);
            var valueSrc2Y = dataset.GetPreviousValueY(src2);
            var valueDstX = dataset.GetPreviousValueX(dst);
            var valueDstY = dataset.GetPreviousValueY(dst);
            
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
            
            dataset.ShrinkSizeTo(dst, iDst);
        }
    }
}