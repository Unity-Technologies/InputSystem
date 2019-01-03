using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.UI;

using UnityEngine.Experimental.Input.Utilities;

public class IntegerControlActionStatus : MonoBehaviour
{
    public InputAction IntegerAction;

    public Text statusText;

    void OnEnable()
    {
        IntegerAction.performed += UpdateInteger;
        IntegerAction.started += UpdateInteger;
        IntegerAction.cancelled += UpdateInteger;
        IntegerAction.Enable();

        ReadOnlyArray<InputControl> controls = IntegerAction.controls;
        for (int i = 0; i < controls.Count; i++)
        {
            statusText.text = controls[i].ReadValueAsObject().ToString();
        }
    }

    void OnDisable()
    {
        IntegerAction.Disable();
        IntegerAction.performed -= UpdateInteger;
        IntegerAction.started -= UpdateInteger;
        IntegerAction.cancelled -= UpdateInteger;
    }

    private void UpdateInteger(InputAction.CallbackContext context)
    {
        int value = ((IntegerControl)(context.control)).ReadValue();
        statusText.text = value.ToString();
    }
}
