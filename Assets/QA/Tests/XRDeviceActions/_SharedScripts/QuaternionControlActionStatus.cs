using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.UI;

public class QuaternionControlActionStatus : MonoBehaviour
{
    public InputAction quaternionAction;

    public Text statusText;

    // Use this for initialization
    void OnEnable()
    {
        quaternionAction.Enable();
        quaternionAction.performed += UpdateQuaternion;
        quaternionAction.started += UpdateQuaternion;
        quaternionAction.cancelled += UpdateQuaternion;
    }

    void OnDisable()
    {
        quaternionAction.Disable();
        quaternionAction.performed -= UpdateQuaternion;
        quaternionAction.started -= UpdateQuaternion;
        quaternionAction.cancelled -= UpdateQuaternion;
    }

    private void UpdateQuaternion(InputAction.CallbackContext context)
    {
        Quaternion value = ((QuaternionControl)(context.control)).ReadValue();
        statusText.text = QuaternionToFieldText(value);
    }

    private string QuaternionToFieldText(Quaternion inQuat)
    {
        return inQuat.ToString("+0.000;-0.000;+0.000");
    }
}
