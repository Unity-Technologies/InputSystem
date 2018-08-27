using Steamworks;

#if UNITY_STANDALONE

////REVIEW: this should ideally be part of the itself system itself but we can't reference the Steamworks.NET
////        API from there; find another packaging solution?

namespace UnityEngine.Experimental.Input.Plugins.Steam
{
    /// <summary>
    /// Implementation of <see cref="ISteamControllerAPI"/> for <see href="https://steamworks.github.io/">
    /// Steamworks.NET</see>.
    /// </summary>
    public class SteamworksNETControllerAPI : ISteamControllerAPI
    {
        public SteamworksNETControllerAPI()
        {
            if (!Steamworks.SteamController.Init())
                Debug.LogError("Could not initialize SteamController API");
        }

        public void RunFrame()
        {
            Steamworks.SteamController.RunFrame();
        }

        public int GetConnectedControllers(ulong[] outHandles)
        {
            if (m_ConnectedControllers == null)
                m_ConnectedControllers = new ControllerHandle_t[Constants.STEAM_CONTROLLER_MAX_COUNT];
            var controllerCount = Steamworks.SteamController.GetConnectedControllers(m_ConnectedControllers);
            for (var i = 0; i < controllerCount; ++i)
                outHandles[i] = m_ConnectedControllers[i].m_ControllerHandle;
            return controllerCount;
        }

        public ulong GetActionSetHandle(string actionSetName)
        {
            return Steamworks.SteamController.GetActionSetHandle(actionSetName).m_ControllerActionSetHandle;
        }

        public ulong GetDigitalActionHandle(string actionName)
        {
            return Steamworks.SteamController.GetDigitalActionHandle(actionName).m_ControllerDigitalActionHandle;
        }

        public ulong GetAnalogActionHandle(string actionName)
        {
            return Steamworks.SteamController.GetAnalogActionHandle(actionName).m_ControllerAnalogActionHandle;
        }

        private ControllerHandle_t[] m_ConnectedControllers;
    }
}

#endif // UNITY_STANDALONE
