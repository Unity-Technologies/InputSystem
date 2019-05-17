using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

public class IntegerControlActionStatus : MonoBehaviour
{
    public InputAction IntegerAction;

    public Text statusText;

    void OnEnable()
    {
        IntegerAction.performed += UpdateInteger;
        IntegerAction.started += UpdateInteger;
        IntegerAction.canceled += UpdateInteger;
        IntegerAction.Enable();

        ReadOnlyArray<InputControl> controls = IntegerAction.controls;
        for (int i = 0; i < controls.Count; i++)
        {
            IntegerControl control = controls[i] as IntegerControl;
            if (control != null)
            {
                int value = control.ReadValue();
                statusText.text = value.ToString();
            }
            else
            {
                Debug.LogWarningFormat(this, "IntegerControlActionStatus expects bindings of type {1}, but found {1} binding named {2}.", typeof(IntegerControl).FullName, controls[i].GetType().FullName, controls[i].name);
            }
        }
    }

    void OnDisable()
    {
        IntegerAction.Disable();
        IntegerAction.performed -= UpdateInteger;
        IntegerAction.started -= UpdateInteger;
        IntegerAction.canceled -= UpdateInteger;
    }

    private void UpdateInteger(InputAction.CallbackContext context)
    {
        IntegerControl control = context.control as IntegerControl;
        if (control != null)
        {
            int value = control.ReadValue();
            statusText.text = value.ToString();
        }
    }
}
