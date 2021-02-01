#if UNITY_EDITOR && UNITY_2021_1_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEditor.DeviceSimulation;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputSystemPlugin : DeviceSimulatorPlugin
    {
        internal Touchscreen SimulatorTouchscreen;

        private bool m_InputSystemEnabled;
        private List<InputDevice> m_DisabledDevices;

        public override string title => "Input System";

        public override void OnCreate()
        {
            m_InputSystemEnabled = EditorPlayerSettingHelpers.newSystemBackendsEnabled;
            if (m_InputSystemEnabled)
            {
                m_DisabledDevices = new List<InputDevice>();

                // deviceSimulator is never null when the plugin is instantiated by a simulator window, but it can be null during unit tests
                if (deviceSimulator != null)
                    deviceSimulator.touchScreenInput += OnTouchEvent;
                InputSystem.onDeviceChange += OnDeviceChange;

                // UGUI elements like a button don't get pressed when multiple pointers for example mouse and touchscreen are sending data at the same time
                foreach (var device in InputSystem.devices)
                {
                    DisableConflictingDevice(device);
                }

                SimulatorTouchscreen = InputSystem.AddDevice<Touchscreen>("Device Simulator Touchscreen");
            }
        }

        internal void OnTouchEvent(TouchEvent touchEvent)
        {
            // Input System does not accept 0 as id
            var id = touchEvent.touchId + 1;

            InputSystem.QueueStateEvent(SimulatorTouchscreen,
                new TouchState
                {
                    touchId = id,
                    phase = ToInputSystem(touchEvent.phase),
                    position = touchEvent.position
                });
        }

        private void DisableConflictingDevice(InputDevice device)
        {
            if (device.native && (device is Mouse || device is Pen) && device.enabled)
            {
                InputSystem.DisableDevice(device);
                m_DisabledDevices.Add(device);
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
                DisableConflictingDevice(device);
        }

        private static UnityEngine.InputSystem.TouchPhase ToInputSystem(UnityEditor.DeviceSimulation.TouchPhase original)
        {
            switch (original)
            {
                case UnityEditor.DeviceSimulation.TouchPhase.Began:
                    return UnityEngine.InputSystem.TouchPhase.Began;
                case UnityEditor.DeviceSimulation.TouchPhase.Moved:
                    return UnityEngine.InputSystem.TouchPhase.Moved;
                case UnityEditor.DeviceSimulation.TouchPhase.Ended:
                    return UnityEngine.InputSystem.TouchPhase.Ended;
                case UnityEditor.DeviceSimulation.TouchPhase.Canceled:
                    return UnityEngine.InputSystem.TouchPhase.Canceled;
                case UnityEditor.DeviceSimulation.TouchPhase.Stationary:
                    return UnityEngine.InputSystem.TouchPhase.Stationary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(original), original, "Unexpected value");
            }
        }

        public override void OnDestroy()
        {
            if (m_InputSystemEnabled)
            {
                // deviceSimulator is never null when the plugin is instantiated by a simulator window, but it can be null during unit tests
                if (deviceSimulator != null)
                    deviceSimulator.touchScreenInput -= OnTouchEvent;
                InputSystem.onDeviceChange -= OnDeviceChange;

                if (SimulatorTouchscreen != null)
                    InputSystem.RemoveDevice(SimulatorTouchscreen);
                foreach (var device in m_DisabledDevices)
                {
                    if (device.added)
                        InputSystem.EnableDevice(device);
                }
            }
        }
    }
}

#endif
