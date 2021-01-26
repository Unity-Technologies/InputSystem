#if UNITY_EDITOR && UNITY_2021_1_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.DeviceSimulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class InputSystemPlugin : DeviceSimulatorPlugin
{
    private bool m_InputSystemEnabled;
    private Touchscreen m_SimulatorTouchscreen;
    private List<InputDevice> m_DisabledDevices;

    public override string title => "Input System";

    public override void OnCreate()
    {
        var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>()[0];
        SerializedObject playerSettingsSerialized = new SerializedObject(playerSettings);

        var activeInputHandler = playerSettingsSerialized.FindProperty("activeInputHandler").intValue;
        m_InputSystemEnabled = activeInputHandler == 1 || activeInputHandler == 2;
        if (m_InputSystemEnabled)
        {
            deviceSimulator.touchScreenInput += OnTouchEvent;

            // UGUI elements like a button don't get pressed when multiple pointers for example mouse and touchscreen are sending data at the same time
            m_DisabledDevices = new List<InputDevice>();
            foreach (var device in InputSystem.devices)
            {
                if (device.native && device is Mouse && device.enabled)
                {
                    InputSystem.DisableDevice(device);
                    m_DisabledDevices.Add(device);
                }
            }
            m_SimulatorTouchscreen = InputSystem.AddDevice<Touchscreen>("Device Simulator Touchscreen");
        }
    }

    private void OnTouchEvent(TouchEvent touchEvent)
    {
        // Input System does not accept 0 as id
        var id = touchEvent.touchId + 1;

        InputSystem.QueueStateEvent(m_SimulatorTouchscreen,
            new TouchState
            {
                touchId = id,
                phase = ToInputSystem(touchEvent.phase),
                position = touchEvent.position
            });
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
            deviceSimulator.touchScreenInput -= OnTouchEvent;
            if (m_SimulatorTouchscreen != null)
                InputSystem.RemoveDevice(m_SimulatorTouchscreen);
            foreach (var device in m_DisabledDevices)
            {
                InputSystem.EnableDevice(device);
            }
        }
    }
}

#endif
