using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.UI;

public class AxisControlActionStatus : MonoBehaviour
{
    public InputAction axisAction;

    public Slider statusSlider;

    void OnEnable()
    {
        axisAction.Enable();
        axisAction.performed += UpdateAxis;
        axisAction.started += UpdateAxis;
        axisAction.cancelled += UpdateAxis;

        ReadOnlyArray<InputControl> controls = axisAction.controls;
        for (int i = 0; i < controls.Count; i++)
        {
            AxisControl control = controls[i] as AxisControl;
            if (control != null)
            {
                float value = control.ReadValue();
                statusSlider.value = value;
            }
            else
            {
                Debug.LogWarningFormat(this, "AxisControlActionStatus expects bindings of type {1}, but found {1} binding named {2}.", typeof(AxisControl).FullName, controls[i].GetType().FullName, controls[i].name);
            }
        }
    }

    private void OnDisable()
    {
        axisAction.Disable();
        axisAction.performed -= UpdateAxis;
        axisAction.started -= UpdateAxis;
        axisAction.cancelled -= UpdateAxis;
    }

    private void UpdateAxis(InputAction.CallbackContext context)
    {
        AxisControl control = context.control as AxisControl;
        if (control != null)
        {
            float value = control.ReadValue();
            statusSlider.value = value;
        }
    }
}
