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
    private InputAction[] actionsForReadValue; 
    private InputAction[] actionsWithCallbacks; 

    //public Mode mode = Mode.ReadValueFromControl;

    // private void DisposeActions(ref InputAction[] actions)
    // {
    //     if (actions == null)
    //         return;
    //     
    //     foreach (var action in actions)
    //     {
    //         action.Disable();
    //         action.Dispose();
    //     }
    //
    //     actions = null;
    // }

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
                //DisposeActions(ref actionsForReadValue);
                //DisposeActions(ref actionsWithCallbacks);

                for (var i = 0; i < controls.Length; ++i)
                    poses[i] = controls[i].ReadValue();

                break;
            case Mode.ReadValueFromActions:
                // DisposeActions(ref actionsWithCallbacks);
                //
                // if (actionsForReadValue == null)
                // {
                //     
                // }

                
                break;
            case Mode.CallbacksFromActions:
                // DisposeActions(ref actionsForReadValue);

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
