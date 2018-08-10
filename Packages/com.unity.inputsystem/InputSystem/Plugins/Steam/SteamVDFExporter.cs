#if UNITY_EDITOR && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using System;

////REVIEW: What we really want, I think, is a *layout* in the system that represents the device
////        with the actions being controls. Exporting InputActions to .VDF seems to not make much sense

namespace UnityEngine.Experimental.Input.Plugins.Steam.Editor
{
    /// <summary>
    /// Exports an .inputactions asset to Steam .VDF format.
    /// </summary>
    public static class SteamVDFExporter
    {
        public static string ConvertInputActionsToVDF()
        {
            throw new NotImplementedException();
        }
    }
}

#endif // UNITY_EDITOR && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
