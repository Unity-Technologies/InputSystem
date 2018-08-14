using Steamworks;

#if UNITY_STANDALONE

////REVIEW: this should ideally be part of the itself system itself but we can't reference the Steamworks.NET
////        API from there; find another packaging solution?

namespace UnityEngine.Experimental.Input.Plugins.Steam
{
    public class SteamworksNETControllerAPI : ISteamControllerAPI
    {
        private ControllerHandle_t[] m_ConnectedControllers;
        private SteamController[] m_InputDevices;

        public SteamworksNETControllerAPI()
        {
        }

        public void Update()
        {
        }
    }
}

#endif // UNITY_STANDALONE
