using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
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
        float value = ((AxisControl)(context.control)).ReadValue();
        statusSlider.value = value;
    }
}
