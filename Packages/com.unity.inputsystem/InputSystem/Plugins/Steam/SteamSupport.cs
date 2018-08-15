#if (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT

namespace UnityEngine.Experimental.Input.Plugins.Steam
{
    public static class SteamSupport
    {
        public static void Initialize()
        {
            // We use this as a base layout.
            InputSystem.RegisterLayout<SteamController>();
        }
    }
}

#endif // (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
