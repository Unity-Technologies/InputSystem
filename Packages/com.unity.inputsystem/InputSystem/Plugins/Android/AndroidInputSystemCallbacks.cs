#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Android.LowLevel;

namespace UnityEngine.InputSystem.Android
{
    class InputSystemCallbacks : AndroidJavaProxy
    {
        public InputSystemCallbacks()
            : base("com.unity.inputsystem.AndroidInputSystem$IInputSystemCallbacks")
        { }

        int AddDevice(string deviceClass)
        {
            var description = new InputDeviceDescription();
            description.deviceClass = deviceClass;
            var device = InputSystem.AddDevice(description);
            return device.deviceId;
        }

        void RemoveDevice(int deviceId)
        {
            var device = InputSystem.GetDeviceById(deviceId);
            if (device == null)
                throw new Exception($"Cannot remove device, device with id {deviceId} doesn't exist");
            InputSystem.RemoveDevice(device);
        }

        void QueueScreenKeyboardEvent(int deviceId, int state, float occludingAreaPositionX, float occludingAreaPositionY, float occludingAreaSizeX, float occludingAreaSizeY, string text)
        {
            var inputEvent = ScreenKeyboardEvent.Create(deviceId, 
                new ScreenKeyboardProperties()
                {
                    State = (ScreenKeyboardState) state,
                    OccludingArea = new Rect(occludingAreaPositionX, occludingAreaPositionY, occludingAreaSizeX, occludingAreaSizeY)
                },
                InputRuntime.s_Instance.currentTime);
            InputSystem.QueueEvent(ref inputEvent);
        }
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
