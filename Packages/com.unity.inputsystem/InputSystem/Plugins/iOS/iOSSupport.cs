#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.iOS
{
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class iOSSupport
    {
        private static iOSScreenKeyboard m_iOSScreenKeyboard;

        public static void Initialize()
        {
            InputSystem.RegisterLayout<iOSGameController>("iOSGameController",
                matches: new InputDeviceMatcher()
                    .WithInterface("iOS")
                    .WithDeviceClass("iOSGameController"));

            InputSystem.RegisterLayout<XboxOneGampadiOS>("XboxOneGampadiOS",
                matches: new InputDeviceMatcher()
                    .WithInterface("iOS")
                    .WithDeviceClass("iOSGameController")
                    .WithProduct("Xbox Wireless Controller"));

            InputSystem.RegisterLayout<DualShock4GampadiOS>("DualShock4GampadiOS",
                matches: new InputDeviceMatcher()
                    .WithInterface("iOS")
                    .WithDeviceClass("iOSGameController")
                    .WithProduct("DUALSHOCK 4 Wireless Controller"));

            InputSystem.RegisterLayoutMatcher("GravitySensor",
                new InputDeviceMatcher()
                    .WithInterface("iOS")
                    .WithDeviceClass("Gravity"));
            InputSystem.RegisterLayoutMatcher("AttitudeSensor",
                new InputDeviceMatcher()
                    .WithInterface("iOS")
                    .WithDeviceClass("Attitude"));
            InputSystem.RegisterLayoutMatcher("LinearAccelerationSensor",
                new InputDeviceMatcher()
                    .WithInterface("iOS")
                    .WithDeviceClass("LinearAcceleration"));

            #if !UNITY_EDITOR
            // iOSSupport.Initialize can be called multiple times when running tests
            // Reuse screen keyboard
            if (m_iOSScreenKeyboard == null)
                m_iOSScreenKeyboard = new iOSScreenKeyboard();
            NativeInputRuntime.instance.screenKeyboard = m_iOSScreenKeyboard();
            #endif
        }

        public static void Shutdown()
        {
            #if !UNITY_EDITOR
            NativeInputRuntime.instance.screenKeyboard = null;
            #endif
        }
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
