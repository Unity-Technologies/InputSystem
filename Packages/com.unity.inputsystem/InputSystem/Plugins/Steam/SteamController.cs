namespace UnityEngine.Experimental.Input.Plugins.Steam
{
    /// <summary>
    /// Base class for controllers made available through the Steam controller API.
    /// </summary>
    /// <remarks>
    /// Unlike other controllers, the Steam controller is somewhat of an amorphic input
    /// device which gains specfic shape only in combination with VDF files. These files
    /// specify the actions that are supported by the application and are internally bound
    /// to specific controls on a controller inside the Steam runtime.
    ///
    /// Note that as the Steam controller API supports PS4 and Xbox controllers as well,
    /// the actual hardware device behind a SteamController instance may not be a
    /// Steam Controller.
    /// </remarks>
    public class SteamController : InputDevice
    {
    }

    //add support for constructing InputControlLayouts from Steam VDF files
}
