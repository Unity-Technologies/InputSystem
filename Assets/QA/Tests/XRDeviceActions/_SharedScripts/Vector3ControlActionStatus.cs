using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
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

        ReadOnlyArray<InputControl> controls = vector3Action.controls;
        for (int i = 0; i < controls.Count; i++)
        {
            Vector3Control control = controls[i] as Vector3Control;
            if (control != null)
            {
                Vector3 value = control.ReadValue();
                statusText.text = Vector3ToFieldText(value);
            }
            else
            {
                Debug.LogWarningFormat(this, "Vector3ControlActionStatus expects bindings of type {1}, but found {1} binding named {2}.", typeof(Vector3Control).FullName, controls[i].GetType().FullName, controls[i].name);
            }
        }
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
        Vector3Control control = context.control as Vector3Control;
        if (control != null)
        {
            Vector3 value = control.ReadValue();
            statusText.text = Vector3ToFieldText(value);
        }
    }

    private string Vector3ToFieldText(Vector3 inVec)
    {
        return inVec.ToString("+000.000;-000.000;+000.000");
    }
}
