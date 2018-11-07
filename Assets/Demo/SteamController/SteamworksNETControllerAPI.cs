#if UNITY_STANDALONE && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using Steamworks;

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

        public int GetConnectedControllers(SteamHandle<SteamController>[] outHandles)
        {
            if (m_ConnectedControllers == null)
                m_ConnectedControllers = new ControllerHandle_t[Constants.STEAM_CONTROLLER_MAX_COUNT];
            var controllerCount = Steamworks.SteamController.GetConnectedControllers(m_ConnectedControllers);
            for (var i = 0; i < controllerCount; ++i)
                outHandles[i] = new SteamHandle<SteamController>((ulong)m_ConnectedControllers[i]);
            return controllerCount;
        }

        public SteamHandle<InputActionMap> GetActionSetHandle(string actionSetName)
        {
            return new SteamHandle<InputActionMap>(
                (ulong)Steamworks.SteamController.GetActionSetHandle(actionSetName));
        }

        public SteamHandle<InputAction> GetDigitalActionHandle(string actionName)
        {
            return new SteamHandle<InputAction>((ulong)Steamworks.SteamController.GetDigitalActionHandle(actionName));
        }

        public SteamHandle<InputAction> GetAnalogActionHandle(string actionName)
        {
            return new SteamHandle<InputAction>((ulong)Steamworks.SteamController.GetAnalogActionHandle(actionName));
        }

        public void ActivateActionSet(SteamHandle<SteamController> controllerHandle, SteamHandle<InputActionMap> actionSetHandle)
        {
            Steamworks.SteamController.ActivateActionSet(new ControllerHandle_t((ulong)actionSetHandle),
                new ControllerActionSetHandle_t((ulong)actionSetHandle));
        }

        public SteamHandle<InputActionMap> GetCurrentActionSet(SteamHandle<SteamController> controllerHandle)
        {
            throw new System.NotImplementedException();
        }

        public void ActivateActionSetLayer(SteamHandle<SteamController> controllerHandle, SteamHandle<InputActionMap> actionSetLayerHandle)
        {
            throw new System.NotImplementedException();
        }

        public void DeactivateActionSetLayer(SteamHandle<SteamController> controllerHandle, SteamHandle<InputActionMap> actionSetLayerHandle)
        {
            throw new System.NotImplementedException();
        }

        public void DeactivateAllActionSetLayers(SteamHandle<SteamController> controllerHandle)
        {
            throw new System.NotImplementedException();
        }

        public int GetActiveActionSetLayers(SteamHandle<SteamController> controllerHandle,
            out SteamHandle<InputActionMap> handlesOut)
        {
            throw new System.NotImplementedException();
        }

        public SteamAnalogActionData GetAnalogActionData(SteamHandle<SteamController> controllerHandle,
            SteamHandle<InputAction> analogActionHandle)
        {
            throw new System.NotImplementedException();
        }

        public SteamDigitalActionData GetDigitalActionData(SteamHandle<SteamController> controllerHandle,
            SteamHandle<InputAction> digitalActionHandle)
        {
            throw new System.NotImplementedException();
        }

        private ControllerHandle_t[] m_ConnectedControllers;
    }
}

#endif // UNITY_STANDALONE && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
