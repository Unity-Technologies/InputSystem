#if UNITY_EDITOR || UNITY_SWITCH
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Plugins.Switch
{
    /// <summary>
    /// Adds support for Switch NPad controllers.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class SwitchSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterLayout<NPad>(
                matches: new InputDeviceMatcher()
                    .WithInterface("Switch")
                    .WithManufacturer("Nintendo")
                    .WithProduct("Wireless Controller"));
        }
    }
}
#endif // UNITY_EDITOR || UNITY_SWITCH
