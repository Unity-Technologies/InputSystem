#if UNITY_EDITOR
using ISX.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ISX.Processors
{
    /// <summary>
    /// If Unity is currently in an EditorWindow callback, transforms a 2D coordinate from
    /// player window space into window space of the current EditorWindow.
    /// </summary>
    /// <remarks>
    /// This processor is only available in the editor. Also, it only works on devices that
    /// support the <see cref="EditorWindowPosConfig"/> request.
    ///
    /// Outside of EditorWindow callbacks, this processor does nothing and just passes through
    /// the coordinates it receives.
    /// </remarks>
    public class EditorWindowSpaceProcessor : IInputProcessor<Vector2>
    {
        public static FourCC EditorWindowPosConfig = new FourCC('E', 'W', 'P', 'S');

        public unsafe Vector2 Process(Vector2 position, InputControl control)
        {
            var bufferSize = sizeof(Vector2);
            var buffer = UnsafeUtility.Malloc((ulong)bufferSize, 4, Allocator.Temp);
            try
            {
                // Write input coordinates.
                *((Vector2*)buffer) = position;

                // Request conversion from device.
                var device = control.device;
                var numBytesRead = device.ReadData(EditorWindowPosConfig, buffer, bufferSize);
                if (numBytesRead < bufferSize)
                    return position;

                // Return converted coordinates.
                return *((Vector2*)buffer);
            }
            finally
            {
                UnsafeUtility.Free(buffer, Allocator.Temp);
            }
        }
    }
}
#endif // UNITY_EDITOR
