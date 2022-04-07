#if true // UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS
using UnityEngine.InputSystem.Apple.LowLevel;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Apple
{
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class AppleSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterLayout<AppleGameController>("AppleGameController",
                matches: new InputDeviceMatcher()
                    .WithInterface("AppleGameController")
                    .WithDeviceClass("GCController"));
        }
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
