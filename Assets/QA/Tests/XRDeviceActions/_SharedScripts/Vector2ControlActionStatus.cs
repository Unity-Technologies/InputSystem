using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.UI;

public class Vector2ControlActionStatus : MonoBehaviour
{
    public InputAction vector2Action;

    public Slider status1Slider;
    public Slider status2Slider;

    void OnEnable()
    {
        vector2Action.Enable();
        vector2Action.performed += UpdateVector2;
        vector2Action.started += UpdateVector2;
        vector2Action.cancelled += UpdateVector2;

        ReadOnlyArray<InputControl> controls = vector2Action.controls;
        for (int i = 0; i < controls.Count; i++)
        {
            Vector2Control control = controls[i] as Vector2Control;
            if (control != null)
            {
                Vector2 value = control.ReadValue();
                status1Slider.value = value.x;
                status2Slider.value = value.y;
            }
            else
            {
                Debug.LogWarningFormat(this, "Vector2ControlActionStatus expects bindings of type {1}, but found {1} binding named {2}.", typeof(Vector2Control).FullName, controls[i].GetType().FullName, controls[i].name);
            }
        }
    }

    void OnDisable()
    {
        vector2Action.Disable();
        vector2Action.performed -= UpdateVector2;
        vector2Action.started -= UpdateVector2;
        vector2Action.cancelled -= UpdateVector2;
    }

    private void UpdateVector2(InputAction.CallbackContext context)
    {
        Vector2Control control = context.control as Vector2Control;
        if (control != null)
        {
            Vector2 value = control.ReadValue();
            status1Slider.value = value.x;
            status2Slider.value = value.y;
        }
    }
}
