#if UNITY_EDITOR || UNITY_SWITCH
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.Switch
{
    /// <summary>
    /// Adds support for Switch NPad controllers.
    /// </summary>
    public static class SwitchSupport
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
