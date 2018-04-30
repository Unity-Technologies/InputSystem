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
    public Text statusText;

    // Use this for initialization
    void Start()
    {
        axisAction.Enable();
        axisAction.performed += UpdateAxis;
        axisAction.started += UpdateAxis;
        axisAction.cancelled += UpdateAxis;
    }

    private void UpdateAxis(InputAction.CallbackContext context)
    {
        float value = ((AxisControl)(context.control)).ReadValue();
        statusSlider.value = value;

        // 2018-04-30 Jack Pritz
        // This is commented out because it causes an error due to
        // https://github.com/StayTalm/InputSystem/issues/9
        // statusText.text = value.ToString();
    }
}
