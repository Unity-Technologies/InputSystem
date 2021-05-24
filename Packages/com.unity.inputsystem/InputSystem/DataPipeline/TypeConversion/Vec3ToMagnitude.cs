using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.InputSystem.DataPipeline.TypeConversion
{
    /*
    // Converts 3 dimensional vector to single float magnitude.
    // N->N conversion.
    internal unsafe struct Vec3ToMagnitude : IDataPipelineStep
    {
        [ReadOnly] [NoAlias] public float* srcX;
    
        [ReadOnly] [NoAlias] public float* srcY;

        [ReadOnly] [NoAlias] public float* srcZ;
    
        [ReadOnly] public int* srcLength;
    
        [WriteOnly] [NoAlias] public float* dst;
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            var l = *srcLength;
            for (var i = 0; i < l; ++i)
                dst[i] = new Vector3(srcX[i], srcY[i], srcZ[i]).magnitude;
        }
    }
    */
}