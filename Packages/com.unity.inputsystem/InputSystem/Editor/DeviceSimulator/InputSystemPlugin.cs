#if UNITY_EDITOR

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
        private bool m_Quitting;
        private List<InputDevice> m_DisabledDevices;

        public override string title => "Input System";

        public override void OnCreate()
        {
            m_InputSystemEnabled = EditorPlayerSettingHelpers.newSystemBackendsEnabled;
            if (m_InputSystemEnabled)
            {
                // Monitor whether the editor is quitting to avoid risking unsafe EnableDevice while quitting
                UnityEditor.EditorApplication.quitting += OnQuitting;

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

                UnityEditor.EditorApplication.quitting -= OnQuitting;

                if (SimulatorTouchscreen != null)
                    InputSystem.RemoveDevice(SimulatorTouchscreen);
                foreach (var device in m_DisabledDevices)
                {
                    // Note that m_Quitting is used here to mitigate the problem reported in issue tracker:
                    // https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-10774.
                    // Enabling a device will call into IOCTL of backend which will (may) be destroyed prior
                    // to this callback on Unity version <= 2022.2. This is not a fix for the actual problem
                    // of shutdown order but a package fix to mitigate this problem.
                    if (device.added && !m_Quitting)
                        InputSystem.EnableDevice(device);
                }
            }
        }

        private void OnQuitting()
        {
            m_Quitting = true;
        }
    }
}

#endif
