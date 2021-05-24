using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.InputSystem.DataPipeline.Collections;

namespace UnityEngine.InputSystem.DataPipeline.TypeConversion
{
    // Converts single integer enum component to single float component.
    // Applies bit mask first, then looks into enum LUT.
    // N->N conversion.
    internal struct EnumToFloat : IPipelineStage 
    {
        public StepFunctionInt src;
        public StepFunction1D dst;
        [ReadOnly] public UnsafeNativeSlice<float> lutUnsafe;

        public int mask;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(DatasetProxy datasetProxy)
        {
            datasetProxy.MapNToN(src, dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(DatasetProxy datasetProxy)
        {
            var l = datasetProxy.MapNToN(src, dst);
            var v = datasetProxy.GetValuesOpaque(src);
            var r = datasetProxy.GetValuesX(dst);
            var lut = lutUnsafe.ToNativeSlice();

            // TODO No SIMD here yet :( do we need AVX-512?
            for (var i = 0; i < l; ++i)
                r[i] = lut[(v[i] & mask)];
        }
    }
}