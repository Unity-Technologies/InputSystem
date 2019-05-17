#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Plugins.iOS
{
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class iOSSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterLayout<iOSGameController>("iOSGameController",
                matches: new InputDeviceMatcher()
                    .WithInterface("iOS")
                    .WithDeviceClass("iOSGameController"));
        }
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
