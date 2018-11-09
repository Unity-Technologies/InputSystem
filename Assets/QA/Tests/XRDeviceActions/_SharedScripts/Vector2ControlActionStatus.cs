using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
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
        Vector2 value = ((Vector2Control)(context.control)).ReadValue();
        status1Slider.value = value.x;
        status2Slider.value = value.y;
    }
}
