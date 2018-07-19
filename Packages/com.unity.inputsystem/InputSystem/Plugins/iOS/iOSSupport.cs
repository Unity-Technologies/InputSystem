#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
namespace UnityEngine.Experimental.Input.Plugins.iOS
{
    public static class iOSSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterControlLayout<iOSGameController>("iOSGameController",
                matches: new InputDeviceMatcher()
                    .WithInterface("iOS")
                    .WithDeviceClass("iOSGameController"));
        }
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
