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
            InputSystem.RegisterControlLayout<NPad>(
                matches: new InputDeviceMatcher()
                .WithInterface("NPad")     ////TODO: this should be 'Switch'
                .WithManufacturer("Nintendo")
                .WithProduct("Wireless Controller"));
        }
    }
}
#endif // UNITY_EDITOR || UNITY_SWITCH
