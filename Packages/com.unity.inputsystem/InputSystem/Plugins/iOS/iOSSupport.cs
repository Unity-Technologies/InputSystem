#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
namespace ISX.Plugins.iOS
{
    public static class IOSSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<IOSGameController>("iOSGameController");
        }
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
