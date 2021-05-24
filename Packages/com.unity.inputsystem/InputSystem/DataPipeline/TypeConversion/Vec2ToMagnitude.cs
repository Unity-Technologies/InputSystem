using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.TypeConversion
{
    // Converts 2 dimensional vector to single float magnitude.
    // N->N conversion.
    internal struct Vec2ToMagnitude : IPipelineStage
    {
        public StepFunction2D src;
        public StepFunction1D dst;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(DatasetProxy datasetProxy)
        {
            datasetProxy.MapNToN(src, dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(DatasetProxy datasetProxy)
        {
            var l = datasetProxy.MapNToN(src, dst);
            var vx = datasetProxy.GetValuesX(src);
            var vy = datasetProxy.GetValuesY(src);
            var r = datasetProxy.GetValuesX(dst);

            for (var i = 0; i < l; ++i)
                // TODO use Unity.Mathematics to avoid doubles!
                r[i] = new Vector2(vx[i], vy[i]).magnitude;
        }
    }
}