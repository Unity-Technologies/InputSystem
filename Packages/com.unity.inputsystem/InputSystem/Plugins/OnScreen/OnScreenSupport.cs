#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_WSA
namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    /// <summary>
    /// Support for various forms of on-screen controls.
    /// </summary>
    /// <remarks>
    /// On-screen input visually represents control elements either through (potentially) built-in
    /// mechanisms like <see cref="OnScreenKeyboard"/> or through manually arranged control setups
    /// in the form of <see cref="OnScreenControl">OnScreenControls</see>.
    /// </remarks>
    public static class OnScreenSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterLayout<OnScreenKeyboard>();
        }
    }
}
#endif
