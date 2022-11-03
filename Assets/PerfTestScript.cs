using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class PerfTestScript : MonoBehaviour
{
    public enum Mode
    {
        ReadValueFromControl = 0,
        ReadValueFromActions,
        CallbacksFromActions
    }

    private PoseState[] poses;
    private InputActionMap actionMapForReadValue;
    private InputAction[] actionsForReadValue;
    private InputActionMap actionMapWithCallbacks; 

    //public Mode mode = Mode.ReadValueFromActions;

    private void DisposeActionMap(ref InputActionMap actionMap)
    {
        if (actionMap == null)
            return;
        
        actionMap.Disable();
        actionMap.Dispose();
        actionMap = null;
    }

    static readonly ProfilerMarker s_Marker_ReadValuesFromControls = new ProfilerMarker("PerfTest.ReadValuesFromControls");
    static readonly ProfilerMarker s_Marker_ReadValueFromActions = new ProfilerMarker("PerfTest.ReadValueFromActions");
    //static readonly ProfilerMarker s_Marker_CallbacksFromActions = new ProfilerMarker("PerfTest.CallbacksFromActions");

    void Update()
    {
        if (PerformanceTestDevice.current == null)
            return;

        var devices = InputSystem.devices.OfType<PerformanceTestDevice>().ToArray();
        var controls = devices.SelectMany(x => x.poses).ToArray();

        var recreateActions = false;
        if (poses == null || poses.Length != controls.Length)
        {
            poses = new PoseState[controls.Length];
            recreateActions = true;
        }

        var mode = Mode.CallbacksFromActions;
        switch (mode)
        {
            case Mode.ReadValueFromControl:
                actionsForReadValue = null;
                DisposeActionMap(ref actionMapForReadValue);
                DisposeActionMap(ref actionMapWithCallbacks);

                using (s_Marker_ReadValuesFromControls.Auto())
                {
                    for (var i = 0; i < controls.Length; ++i)
                        poses[i] = controls[i].ReadValue();
                }

                break;
            case Mode.ReadValueFromActions:
                DisposeActionMap(ref actionMapWithCallbacks);
                
                if (actionMapForReadValue == null || recreateActions)
                {
                    actionsForReadValue = null;
                    DisposeActionMap(ref actionMapForReadValue);

                    for (var i = 0; i < devices.Length; ++i)
                        InputSystem.SetDeviceUsage(devices[i], $"perfDevice{i}");
                    
                    actionMapForReadValue = new InputActionMap("actionMapForReadValue");
                    actionsForReadValue = new InputAction[controls.Length]; // action per control

                    for (var i = 0; i < actionsForReadValue.Length; ++i)
                    {
                        var controlIndex = i % PerformanceTestDeviceState.kPoseCount;
                        var deviceIndex = i / PerformanceTestDeviceState.kPoseCount;
                        var usageName = $"perfDevice{deviceIndex}";
                        actionsForReadValue[i] = actionMapForReadValue.AddAction($"perfAction{i}", binding: $"<PerformanceTestDevice>{{{usageName}}}/pose{controlIndex}");
                    }

                    actionMapForReadValue.Enable();
                }

                using (s_Marker_ReadValueFromActions.Auto())
                {
                    for (var i = 0; i < actionsForReadValue.Length; ++i)
                        poses[i] = actionsForReadValue[i].ReadValue<PoseState>();
                }


                break;
            case Mode.CallbacksFromActions:
                actionsForReadValue = null;
                DisposeActionMap(ref actionMapForReadValue);

                if (actionMapWithCallbacks == null || recreateActions)
                {
                    DisposeActionMap(ref actionMapWithCallbacks);

                    for (var i = 0; i < devices.Length; ++i)
                        InputSystem.SetDeviceUsage(devices[i], $"perfDevice{i}");
                    
                    actionMapWithCallbacks = new InputActionMap("actionMapForReadValue");

                    for (var i = 0; i < controls.Length; ++i)
                    {
                        var controlIndex = i % PerformanceTestDeviceState.kPoseCount;
                        var deviceIndex = i / PerformanceTestDeviceState.kPoseCount;
                        var usageName = $"perfDevice{deviceIndex}";
                        var action = actionMapWithCallbacks.AddAction($"perfAction{i}", binding: $"<PerformanceTestDevice>{{{usageName}}}/pose{controlIndex}");

                        var actionPoseIndex = i;
                        action.performed += ctx =>
                        {
                            poses[actionPoseIndex] = ctx.ReadValue<PoseState>();
                        };
                    }

                    actionMapWithCallbacks.Enable();
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnGUI()
    {
        if (poses == null)
            return;
        GUI.Label(new Rect(0.0f, 20.0f * 0, 500.0f, 20.0f), $"pose{0}.position = {poses[0].position}");
        GUI.Label(new Rect(0.0f, 20.0f * 1, 500.0f, 20.0f), $"pose{poses.Length - 1}.position = {poses[^1].position}");
    }
}
