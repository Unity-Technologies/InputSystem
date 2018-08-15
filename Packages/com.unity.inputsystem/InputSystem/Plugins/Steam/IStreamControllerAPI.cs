#if (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT

namespace UnityEngine.Experimental.Input.Plugins.Steam
{
    /// <summary>
    /// This is a wrapper around the Steamworks SDK controller API.
    /// </summary>
    public interface ISteamControllerAPI
    {
        int GetConnectedControllers(ulong[] outHandles);
        int GetActionSetHandle(string actionSetName);
        int GetDigitalActionHandle(string actionName);
    }
}

#endif // (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
