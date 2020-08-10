#if UNITY_WEBGL || UNITY_EDITOR || PACKAGE_DOCS_GENERATION

namespace UnityEngine.InputSystem.WebGL
{
    /// <summary>
    /// A Joystick or Gamepad on WebGL that does not have any known mapping.
    /// </summary>
    [Scripting.Preserve]
    public class WebGLJoystick : Joystick
    {
    }
}
#endif // UNITY_WEBGL || UNITY_EDITOR
