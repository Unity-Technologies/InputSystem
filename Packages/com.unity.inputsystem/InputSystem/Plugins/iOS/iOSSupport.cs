#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
namespace UnityEngine.Experimental.Input.Plugins.iOS
{
    public static class IOSSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterControlLayout<IOSGameController>("iOSGameController",
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "iOS",
                deviceClass = "iOSGameController"
            });
        }
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
