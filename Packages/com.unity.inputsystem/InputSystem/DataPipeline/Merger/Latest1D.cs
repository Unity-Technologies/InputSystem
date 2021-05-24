using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.Merger
{
    // Merges two 1D slices into one 1D slice in order of timestamps.
    // x(N)+y(M)->z(N+M) conversion.
    public struct Latest1D : IPipelineStage
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
            var (l1, l2) = datasetProxy.MapNAndMToNPlusM(src1, src2, dst);

            var t1 = datasetProxy.GetTimestamps(src1);
            var t2 = datasetProxy.GetTimestamps(src2);
            var t3 = datasetProxy.GetTimestamps(dst);

            var v1 = datasetProxy.GetValuesX(src1);
            var v2 = datasetProxy.GetValuesX(src2);
            var v3 = datasetProxy.GetValuesX(dst);

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