#if UNITY_EDITOR
using System.ComponentModel;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;

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
    [DesignTimeVisible(false)]
    [Scripting.Preserve]
    internal class EditorWindowSpaceProcessor : InputProcessor<Vector2>
    {
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            if (control == null)
                throw new System.ArgumentNullException(nameof(control));

            // We go and fire trigger QueryEditorWindowCoordinatesCommand regardless
            // of whether we are currently in EditorWindow code or not. The expectation
            // here is that the underlying editor code is in a better position than us
            // to judge whether the conversion should be performed or not. In native code,
            // the IOCTL implementations will early out if they detect that the current
            // EditorWindow is in fact a game view.

            if (Mouse.s_PlatformMouseDevice != null)
            {
                var command = QueryEditorWindowCoordinatesCommand.Create(value);
                // Not all pointer devices implement the editor window position IOCTL,
                // so we try the global mouse device if available.
                if (Mouse.s_PlatformMouseDevice.ExecuteCommand(ref command) > 0)
                    return command.inOutCoordinates;
            }

            return value;
        }
    }
}
#endif // UNITY_EDITOR
