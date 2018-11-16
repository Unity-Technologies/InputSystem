using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.UI;

public class Vector3ControlActionStatus : MonoBehaviour
{
    public InputAction vector3Action;

    public Text statusText;

    void OnEnable()
    {
        vector3Action.Enable();
        vector3Action.performed += UpdateVector3;
        vector3Action.started += UpdateVector3;
        vector3Action.cancelled += UpdateVector3;
    }

    void OnDisable()
    {
        vector3Action.Disable();
        vector3Action.performed -= UpdateVector3;
        vector3Action.started -= UpdateVector3;
        vector3Action.cancelled -= UpdateVector3;
    }

    private void UpdateVector3(InputAction.CallbackContext context)
    {
        Vector3 value = ((Vector3Control)(context.control)).ReadValue();
        statusText.text = Vector3ToFieldText(value);
    }

    private string Vector3ToFieldText(Vector3 inVec)
    {
        return inVec.ToString("+000.000;-000.000;+000.000");
    }
}
