#if UNITY_EDITOR || UNITY_STANDALONE_OSX
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.OSX
{
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class OSXSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterLayout<NimbusGameController>(
                matches: new InputDeviceMatcher()
                    .WithProduct("Nimbus+"));
        }
    }
}
#endif // UNITY_EDITOR || UNITY_STANDALONE_OSX
