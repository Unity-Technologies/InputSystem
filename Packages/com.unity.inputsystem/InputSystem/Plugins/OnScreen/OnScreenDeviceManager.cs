using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    public class OnScreenDeviceManager
    {
        static OnScreenDeviceManager s_Instance;

        public static OnScreenDeviceManager GetOnScreenDeviceManager()
        {
            if (s_Instance == null)
                s_Instance = new OnScreenDeviceManager();

            return s_Instance;
        }

        public InputEventPtr GetInputEventPtrForDevice(InputDevice device)
        {
            InputEventPtr eventPtr;
            var buffer = StateEvent.From(device, out eventPtr);
            return eventPtr;
        }

        public InputControl SetupInputControl(string controlPath)
        {
            var layout = InputControlPath.TryGetDeviceLayout(controlPath);
            var device = InputSystem.TryGetDevice(layout);
            if (device == null)
                device = InputSystem.AddDevice(layout);

            return InputControlPath.TryFindControl(device, controlPath);
        }
    }
}
