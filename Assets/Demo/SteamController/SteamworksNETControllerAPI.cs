using Steamworks;

#if UNITY_STANDALONE

////REVIEW: this should ideally be part of the itself system itself but we can't reference the Steamworks.NET
////        API from there; find another packaging solution?

namespace UnityEngine.Experimental.Input.Plugins.Steam
{
    public class SteamworksNETControllerAPI : ISteamControllerAPI
    {
        public int GetConnectedControllers(ulong[] outHandles)
        {
            throw new System.NotImplementedException();
        }

        public int GetActionSetHandle(string actionSetName)
        {
            throw new System.NotImplementedException();
        }

        public int GetDigitalActionHandle(string actionName)
        {
            throw new System.NotImplementedException();
        }
    }
}

#endif // UNITY_STANDALONE
