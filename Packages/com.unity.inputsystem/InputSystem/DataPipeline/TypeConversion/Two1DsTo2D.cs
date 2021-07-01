using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.TypeConversion
{
    // Combine two one dimensional values with different timestamp axes into one two dimensional value with one timestamp axis.
    // N->N conversion.
    internal struct Two1DsTo2D : IPipelineStage
    {
        public StepFunction1D srcX, srcY;
        public StepFunction2D dst;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(Dataset dataset)
        {
            dataset.MapNAndMToNPlusM(srcX, srcY, dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Dataset dataset)
        {
            var (l1, l2) = dataset.MapNAndMToNPlusM(srcX, srcY, dst);

            var t1 = dataset.GetTimestamps(srcX);
            var t2 = dataset.GetTimestamps(srcY);
            var t3 = dataset.GetTimestamps(dst);

            var v1 = dataset.GetValuesX(srcX);
            var v2 = dataset.GetValuesX(srcY);
            var rX = dataset.GetValuesX(dst);
            var rY = dataset.GetValuesY(dst);

            var i1 = 0;
            var i2 = 0;
            var i3 = 0;

            var t = dataset.GetPreviousTimestamp(dst);
            var x = dataset.GetPreviousValueX(srcX);
            var y = dataset.GetPreviousValueX(srcY);

            while(i1 < l1 || i2 < l2)
            {
                // merge if both have same timestamp
                if (i1 < l1 && i2 < l2 && t1[i1] == t2[i2])
                {
                    t = t1[i1];
                    x = v1[i1];
                    y = v2[i2];
                    ++i1;
                    ++i2;
                }
                else if (i1 < l1 && (i2 >= l2 || t1[i1] <= t2[i2]))
                {
                    t = t1[i1];
                    x = v1[i1];
                    ++i1;
                }
                else
                {
                    t = t2[i2];
                    y = v2[i2];
                    ++i2;
                }

                t3[i3] = t;
                rX[i3] = x;
                rY[i3] = y;
                ++i3;
            }

            dataset.ShrinkSizeTo(dst, i3);
        }
    }
}