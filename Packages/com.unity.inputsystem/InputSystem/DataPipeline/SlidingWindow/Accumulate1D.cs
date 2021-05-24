using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.SlidingWindow
{
    public struct Accumulate1D : IPipelineStage
    {
        public StepFunction1D src, dst;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(DatasetProxy datasetProxy)
        {
            datasetProxy.MapNToMaxNAndX(src, dst, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(DatasetProxy datasetProxy)
        {
            var prevValue = datasetProxy.GetPreviousValueX(dst);
            // if previous value is 0, and we have 0 incoming values, then no-op
            // if previous value is not 0, and we have 0 incoming values, then allocate ONE new value to set it to 0
            var length = datasetProxy.MapNToMaxNAndX(src, dst, (prevValue == 0.0f) ? 0 : 1);
            var valuesSrc = datasetProxy.GetValuesX(src);
            var valuesDst = datasetProxy.GetValuesX(dst);

            if (valuesSrc.Length > 0)
            {
                // TODO prefix sum on SIMD
                var acc = 0.0f;
                for (var i = 0; i < length; ++i)
                {
                    acc += valuesSrc[i];
                    valuesDst[i] = acc;
                }
            }
            else if (valuesDst.Length > 0)
            {
                var t = datasetProxy.GetTimestamps(dst);
                
                Debug.Assert(length == 1);
                t[0] = datasetProxy.GetPreviousTimestamp(dst) + 1; // TODO this is _probably_ wrong, which timestamp would be better?
                valuesDst[0] = 0.0f;
            }
        }
    }
}