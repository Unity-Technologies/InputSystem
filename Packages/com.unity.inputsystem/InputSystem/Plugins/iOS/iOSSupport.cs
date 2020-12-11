#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using UnityEngine.InputSystem.iOS.LowLevel;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.iOS
{
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class iOSSupport
    {
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

#if UNITY_EDITOR && NOT_WORKING
            // TODO: How to ensure we add device only one time per Editor session?
            //       Since if we call just InputSystem.AddDevice<iOSStepCounter>();
            //       it creates a new device on each enter to play mode
            if (!UnityEditor.SessionState.GetBool(nameof(iOSStepCounter), false))
            {
                Debug.Log("test");
                UnityEditor.SessionState.SetBool(nameof(iOSStepCounter), true);
                InputSystem.AddDevice<iOSStepCounter>();
            }
#else
            // A hack to keep always one step counter, also doesn't work
            /*
            while (InputSystem.GetDevice<iOSStepCounter>() != null)
            {
                InputSystem.RemoveDevice(InputSystem.GetDevice<iOSStepCounter>());
            }
            */
            InputSystem.AddDevice<iOSStepCounter>();
#endif
        }
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
