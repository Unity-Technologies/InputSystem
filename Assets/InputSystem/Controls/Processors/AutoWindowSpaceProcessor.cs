#if UNITY_EDITOR
using ISX.LowLevel;
using UnityEngine;

namespace ISX.Processors
{
    /// <summary>
    /// If Unity is currently in an EditorWindow callback, transforms a 2D coordinate from
    /// player window space into window space of the current EditorWindow.
    /// </summary>
    /// <remarks>
    /// This processor is only available in the editor. Also, it only works on devices that
    /// support the <see cref="QueryEditorWindowCoordinates"/> request.
    ///
    /// Outside of EditorWindow callbacks, this processor does nothing and just passes through
    /// the coordinates it receives.
    /// </remarks>
    public class AutoWindowSpaceProcessor : IInputProcessor<Vector2>
    {
        public unsafe Vector2 Process(Vector2 position, InputControl control)
        {
            var command = QueryEditorWindowCoordinates.Create(position);
            if (control.device.OnDeviceCommand(ref command) > 0)
                return command.inOutCoordinates;
            return position;
        }
    }
}
#endif // UNITY_EDITOR
