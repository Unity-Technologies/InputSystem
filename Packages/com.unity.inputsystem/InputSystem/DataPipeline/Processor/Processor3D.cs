using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.DataPipeline.Processor
{
    // Processes three component value
    // N->N conversion.
    internal struct Processor3D : IPipelineStage
    {
        public StepFunction3D src, dst;

        // if 1.0f value is normalized to 0.0f where 0.0f point is defined by minRange, 1.0f is maxRange
        public float normalize;
        
        // [minRange, maxRange] for clamping magnitude 
        public float minMagnitude, maxMagnitude;

        public float clamp;
        public float clampNormalize;

        // result value scale and offset factor
        public Vector3 scale;
        public Vector3 offset;

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
            var vz = dataset.GetValuesZ(src);
            var rx = dataset.GetValuesX(dst);
            var ry = dataset.GetValuesY(dst);
            var rz = dataset.GetValuesZ(dst);
            
            for (var i = 0; i < l; ++i)
            {
                var x = vx[i];
                var y = vy[i];
                var z = vz[i];
                
                var magOriginal = Mathf.Sqrt(x * x + y * y + z * z);
                var magClamped = Mathf.Clamp(magOriginal, minMagnitude, maxMagnitude);
                var magClampedNormalized = ((magClamped - minMagnitude) / (maxMagnitude - minMagnitude));

                // TODO verify this code!
                var mag = Mathf.LerpUnclamped(1.0f, magOriginal, normalize);
                mag = Mathf.LerpUnclamped(mag, magOriginal / magClamped, clamp);
                mag = Mathf.LerpUnclamped(mag, magOriginal * magClampedNormalized / magClamped, clampNormalize);

                rx[i] = (x / mag) * scale.x + offset.x;
                ry[i] = (y / mag) * scale.y + offset.y;
                rz[i] = (z / mag) * scale.z + offset.z;
            }
        }
    }
}