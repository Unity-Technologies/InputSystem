using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.UI;

public class QuaternionControlActionStatus : MonoBehaviour
{
    public InputAction quaternionAction;

    public Text statusText;

    void OnEnable()
    {
        quaternionAction.Enable();
        quaternionAction.performed += UpdateQuaternion;
        quaternionAction.started += UpdateQuaternion;
        quaternionAction.cancelled += UpdateQuaternion;

        ReadOnlyArray<InputControl> controls = quaternionAction.controls;
        for (int i = 0; i < controls.Count; i++)
        {
            QuaternionControl control = controls[i] as QuaternionControl;
            if (control != null)
            {
                Quaternion value = control.ReadValue();
                statusText.text = QuaternionToFieldText(value);
            }
            else
            {
                Debug.LogWarningFormat(this, "QuaternionControlActionStatus expects bindings of type {1}, but found {1} binding named {2}.", typeof(QuaternionControl).FullName, controls[i].GetType().FullName, controls[i].name);
            }
        }
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
        QuaternionControl control = context.control as QuaternionControl;
        if (control != null)
        {
            Quaternion value = control.ReadValue();
            statusText.text = QuaternionToFieldText(value);
        }
    }

    private string QuaternionToFieldText(Quaternion inQuat)
    {
        return inQuat.ToString("+0.000;-0.000;+0.000");
    }
}
