#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_WSA
namespace UnityEngine.InputSystem.OnScreen
{
    /// <summary>
    /// Support for various forms of on-screen controls.
    /// </summary>
    /// <remarks>
    /// On-screen input visually represents control elements either through (potentially) built-in
    /// mechanisms like <see cref="OnScreenKeyboard"/> or through manually arranged control setups
    /// in the form of <see cref="OnScreenControl">OnScreenControls</see>.
    /// </remarks>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class OnScreenSupport
    {
        public static void Initialize()
        {
            ////TODO: OnScreenKeyboard support
            //InputSystem.RegisterLayout<OnScreenKeyboard>();
        }
    }
}
#endif
