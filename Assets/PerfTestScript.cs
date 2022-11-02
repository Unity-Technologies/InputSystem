using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public Mode mode = Mode.ReadValueFromControl;
    public int actionCount = 100;

     private void DisposeActionMap(ref InputActionMap actionMap)
     {
         if (actionMap == null)
             return;
         
         actionMap.Disable();
         actionMap.Dispose();
         actionMap = null;
     }

    void Update()
    {
        if (PerformanceTestDevice.current == null)
            return;

        var devices = InputSystem.devices.OfType<PerformanceTestDevice>();
        var controls = devices.SelectMany(x => x.poses).ToArray();

        if (poses == null || poses.Length != controls.Length)
            poses = new PoseState[controls.Length];

        var mode = Mode.ReadValueFromControl;
        switch (mode)
        {
            case Mode.ReadValueFromControl:
                actionsForReadValue = null;
                DisposeActionMap(ref actionMapForReadValue);
                DisposeActionMap(ref actionMapWithCallbacks);

                for (var i = 0; i < controls.Length; ++i)
                    poses[i] = controls[i].ReadValue();

                break;
            case Mode.ReadValueFromActions:
                DisposeActionMap(ref actionMapWithCallbacks);
                
                if (actionMapForReadValue == null)
                {
                    actionMapForReadValue = new InputActionMap("actionMapForReadValue");
                    actionsForReadValue = new InputAction[actionCount];

                    for (var i = 0; i < actionCount; ++i)
                        actionsForReadValue[i] = actionMapForReadValue.AddAction($"perfAction{i}", binding: $"<PerformanceTestDevice>/pose{i}");
                    
                    actionMapForReadValue.Enable();
                }

                for (var i = 0; i < actionCount; ++i)
                    poses[i] = actionsForReadValue[i].ReadValue<PoseState>();

                
                break;
            case Mode.CallbacksFromActions:
                actionsForReadValue = null;
                DisposeActionMap(ref actionMapForReadValue);

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnGUI()
    {
        if (poses == null)
            return;
        for (var i = 0; i < 2; ++i)
            GUI.Label(new Rect(0.0f, 20.0f * i, 500.0f, 20.0f), $"pose{i}.position = {poses[i].position}");
    }
}
