#if UNITY_EDITOR || UNITY_SWITCH
namespace UnityEngine.Experimental.Input.Plugins.Switch
{
    /// <summary>
    /// Adds support for Switch NPad controllers.
    /// </summary>
    public static class SwitchSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<NPad>(deviceDescription: new InputDeviceDescription
            {
                interfaceName = "NPad",
                manufacturer = "Nintendo",
                product = "Wireless Controller",
            });
        }
    }
}
#endif // UNITY_EDITOR || UNITY_SWITCH
