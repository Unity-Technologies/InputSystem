using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEngine.InputSystem.DataPipeline.Merger
{
    // Merges two 1D slices into one 1D slice in order of timestamps.
    // x(N)+y(M)->z(N+M) conversion.
    public struct Latest1D : IPipelineStage
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
            var (l1, l2) = dataset.MapNAndMToNPlusM(src1, src2, dst);

            var t1 = dataset.GetTimestamps(src1);
            var t2 = dataset.GetTimestamps(src2);
            var t3 = dataset.GetTimestamps(dst);

            var v1 = dataset.GetValuesX(src1);
            var v2 = dataset.GetValuesX(src2);
            var v3 = dataset.GetValuesX(dst);

            var i1 = 0;
            var i2 = 0;

            for (var i3 = 0; i3 < l1 + l2; ++i3)
            {
                if (i1 < l1 && (i2 >= l2 || t1[i1] <= t2[i2]))
                {
                    t3[i3] = t1[i1];
                    v3[i3] = v1[i1];
                    ++i1;
                }
                else
                {
                    t3[i3] = t2[i2];
                    v3[i3] = v2[i2];
                    ++i2;
                }
            }
        }
    }
}