#if UNITY_EDITOR
using System;
using ISX.Utilities;
using UnityEngine;

namespace ISX.Processors
{
    /// <summary>
    /// If Unity is currently in an EditorWindow callback, transforms a 2D coordinate from
    /// player window space into window space of the current EditorWindow.
    /// </summary>
    /// <remarks>
    /// This processor is only available in the editor. Also, it only works on devices that
    /// support the <see cref="IOCTLGetEditorWindowCoordinates"/> request.
    ///
    /// Outside of EditorWindow callbacks, this processor does nothing and just passes through
    /// the coordinates it receives.
    /// </remarks>
    public class AutoWindowSpaceProcessor : IInputProcessor<Vector2>
    {
        public static FourCC IOCTLGetEditorWindowCoordinates = new FourCC('E', 'W', 'P', 'S');

        public unsafe Vector2 Process(Vector2 position, InputControl control)
        {
            var positionPtr = &position;

            // Request conversion from device.
            var device = control.device;
            var numBytesRead = device.IOCTL(IOCTLGetEditorWindowCoordinates, new IntPtr(positionPtr), sizeof(Vector2));
            if (numBytesRead < sizeof(Vector2))
                return position;

            // Return converted coordinates.
            return *positionPtr;
        }
    }
}
#endif // UNITY_EDITOR
