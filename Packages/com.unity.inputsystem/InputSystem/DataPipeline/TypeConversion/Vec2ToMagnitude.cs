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
        public void Map(Dataset dataset)
        {
            dataset.MapNToN(src, dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Dataset dataset)
        {
            var l = dataset.MapNToN(src, dst);
            var vx = dataset.GetValuesX(src);
            var vy = dataset.GetValuesY(src);
            var r = dataset.GetValuesX(dst);

            for (var i = 0; i < l; ++i)
                // TODO use Unity.Mathematics to avoid doubles!
                r[i] = new Vector2(vx[i], vy[i]).magnitude;
        }
    }
}