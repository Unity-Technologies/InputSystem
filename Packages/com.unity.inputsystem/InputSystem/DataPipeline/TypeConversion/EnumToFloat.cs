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
        // TODO fix me
        //[ReadOnly] public NativeSlice<float> lut;

        public int mask;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(Dataset dataset)
        {
            dataset.MapNToN(src, dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Dataset dataset)
        {
            var l = dataset.MapNToN(src, dst);
            var v = dataset.GetValuesOpaque(src);
            var r = dataset.GetValuesX(dst);

            // TODO No SIMD here yet :( do we need AVX-512?
            //for (var i = 0; i < l; ++i)
            //    r[i] = lut[(v[i] & mask)];
        }
    }
}