using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.Processor
{
    // Processes two component value
    // N->N conversion.
    internal struct Processor2D : IPipelineStage
    {
        public StepFunction2D src, dst;

        // if 1.0f value is normalized to 0.0f where 0.0f point is defined by minRange, 1.0f is maxRange
        public float normalize;
        
        // [minRange, maxRange] for clamping magnitude 
        public float minMagnitude, maxMagnitude;

        public float clamp;
        public float clampNormalize;

        // result value scale and offset factor
        public Vector2 scale;
        public Vector2 offset;

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
            var rx = datasetProxy.GetValuesX(dst);
            var ry = datasetProxy.GetValuesY(dst);
            
            for (var i = 0; i < l; ++i)
            {
                var x = vx[i];
                var y = vy[i];
                
                var magOriginal = Mathf.Sqrt(x * x + y * y);
                var magClamped = Mathf.Clamp(magOriginal, minMagnitude, maxMagnitude);
                var magClampedNormalized = ((magClamped - minMagnitude) / (maxMagnitude - minMagnitude));

                // TODO verify this code!
                var mag = Mathf.LerpUnclamped(1.0f, magOriginal, normalize);
                mag = Mathf.LerpUnclamped(mag, magOriginal / magClamped, clamp);
                mag = Mathf.LerpUnclamped(mag, magOriginal * magClampedNormalized / magClamped, clampNormalize);

                rx[i] = (x / mag) * scale.x + offset.x;
                ry[i] = (y / mag) * scale.y + offset.y;
            }
        }
    }
}