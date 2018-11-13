using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.UI;

public class IntegerControlActionStatus : MonoBehaviour
{
    public InputAction IntegerAction;

    public Text statusText;

    void OnEnable()
    {
        IntegerAction.Enable();
        IntegerAction.performed += UpdateInteger;
        IntegerAction.started += UpdateInteger;
        IntegerAction.cancelled += UpdateInteger;
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
