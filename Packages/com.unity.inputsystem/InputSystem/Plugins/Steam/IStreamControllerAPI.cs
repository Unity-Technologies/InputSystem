#if (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT

namespace UnityEngine.Experimental.Input.Plugins.Steam
{
    /// <summary>
    /// This is a wrapper around the Steamworks SDK controller API.
    /// </summary>
    /// <seealso href="https://partner.steamgames.com/doc/api/ISteamController"/>
    public interface ISteamControllerAPI
    {
        void RunFrame();
        int GetConnectedControllers(ulong[] outHandles);
        ulong GetActionSetHandle(string actionSetName);
        ulong GetDigitalActionHandle(string actionName);
        ulong GetAnalogActionHandle(string actionName);
    }
}

#endif // (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
