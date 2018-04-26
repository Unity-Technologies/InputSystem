#if UNITY_EDITOR
using UnityEngine.Experimental.Input.LowLevel;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Processors
{
    /// <summary>
    /// If Unity is currently in an EditorWindow callback, transforms a 2D coordinate from
    /// player window space into window space of the current EditorWindow.
    /// </summary>
    /// <remarks>
    /// This processor is only available in the editor. Also, it only works on devices that
    /// support the <see cref="QueryEditorWindowCoordinatesCommand"/> request.
    ///
    /// Outside of EditorWindow callbacks, this processor does nothing and just passes through
    /// the coordinates it receives.
    /// </remarks>
    public class EditorWindowSpaceProcessor : IInputControlProcessor<Vector2>
    {
        public Vector2 Process(Vector2 position, InputControl control)
        {
            // Don't convert to EditorWindowSpace if input is going to game view.
            if (InputConfiguration.LockInputToGame ||
                (EditorApplication.isPlaying && Application.isFocused))
                return position;

            var command = QueryEditorWindowCoordinatesCommand.Create(position);
            if (control.device.ExecuteCommand(ref command) > 0)
                return command.inOutCoordinates;
            return position;
        }
    }
}
#endif // UNITY_EDITOR
