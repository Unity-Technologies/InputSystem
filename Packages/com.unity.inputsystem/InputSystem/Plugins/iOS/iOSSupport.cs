#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
namespace UnityEngine.Experimental.Input.Plugins.iOS
{
    public static class IOSSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterControlLayout<IOSGameController>("iOSGameController",
                matches: new InputDeviceMatcher()
                .WithInterface("iOS")
                .WithDeviceClass("iOSGameController"));
        }
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
