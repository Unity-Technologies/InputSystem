using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.SlidingWindow
{
    public struct Accumulate1D : IPipelineStage
    {
        public StepFunction1D src, dst;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(Dataset dataset)
        {
            dataset.MapNToMaxNAndX(src, dst, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Dataset dataset)
        {
            var prevValue = dataset.GetPreviousValueX(dst);
            // if previous value is 0, and we have 0 incoming values, then no-op
            // if previous value is not 0, and we have 0 incoming values, then allocate ONE new value to set it to 0
            var length = dataset.MapNToMaxNAndX(src, dst, (prevValue == 0.0f) ? 0 : 1);
            var valuesSrc = dataset.GetValuesX(src);
            var valuesDst = dataset.GetValuesX(dst);

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
                var t = dataset.GetTimestamps(dst);
                
                Debug.Assert(length == 1);
                t[0] = dataset.GetPreviousTimestamp(dst) + 1; // TODO this is _probably_ wrong, which timestamp would be better?
                valuesDst[0] = 0.0f;
            }
        }
    }
}