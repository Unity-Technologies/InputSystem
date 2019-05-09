#if UNITY_EDITOR
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// If Unity is currently in an <see cref="EditorWindow"/> callback, transforms a 2D coordinate from
    /// player window space into window space of the current EditorWindow.
    /// </summary>
    /// <remarks>
    /// This processor is only available in the editor. Also, it only works on devices that
    /// support the <see cref="QueryEditorWindowCoordinatesCommand"/> request.
    ///
    /// Outside of <see cref="EditorWindow"/> callbacks, this processor does nothing and just passes through
    /// the coordinates it receives.
    /// </remarks>
    /// <seealso cref="Pointer.position"/>
    public class EditorWindowSpaceProcessor : InputProcessor<Vector2>
    {
        public override Vector2 Process(Vector2 position, InputControl<Vector2> control)
        {
            // Don't convert to EditorWindowSpace if input is going to game view.
            if (InputEditorUserSettings.lockInputToGameView ||
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
