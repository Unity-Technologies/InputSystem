using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.UI;

using UnityEngine.Experimental.Input.Utilities;

public class ButtonControlActionStatus : MonoBehaviour
{
    public InputAction buttonPressAction;
    public InputAction buttonTouchAction;

    public Image statusImage;

    private bool m_isTouched;
    private bool m_isPressed;

    void OnEnable()
    {
        buttonTouchAction.performed += UpdateTouchStatus;
        buttonTouchAction.started += UpdateTouchStatus;
        buttonTouchAction.cancelled += UpdateTouchStatus;
        buttonTouchAction.Enable();
        
        ReadOnlyArray<InputControl> controls = buttonTouchAction.controls;
        for (int i = 0; i < controls.Count; i++)
        {
            m_isTouched = (float)(controls[i].ReadValueAsObject()) > 0.5f;
        }
        
        buttonPressAction.performed += UpdatePressStatus;
        buttonPressAction.started += UpdatePressStatus;
        buttonPressAction.cancelled += UpdatePressStatus;
        buttonPressAction.Enable();
        
        controls = buttonPressAction.controls;
        for (int i = 0; i < controls.Count; i++)
        {
            m_isPressed = (float)(controls[i].ReadValueAsObject()) > 0.5f;
        }
        
        ApplyStatusColor();
    }

    void OnDisable()
    {
        buttonTouchAction.Disable();
        buttonTouchAction.performed -= UpdateTouchStatus;
        buttonTouchAction.started -= UpdateTouchStatus;
        buttonTouchAction.cancelled -= UpdateTouchStatus;

        buttonPressAction.Disable();
        buttonPressAction.performed -= UpdatePressStatus;
        buttonPressAction.started -= UpdatePressStatus;
        buttonPressAction.cancelled -= UpdatePressStatus;
    }

    private void UpdatePressStatus(InputAction.CallbackContext context)
    {
        m_isPressed = ((ButtonControl)(context.control)).isPressed;
        ApplyStatusColor();
    }

    private void UpdateTouchStatus(InputAction.CallbackContext context)
    {
        m_isTouched = ((ButtonControl)(context.control)).isPressed;
        ApplyStatusColor();
    }

    private void ApplyStatusColor()
    {
        if (m_isPressed)
        {
            statusImage.color = Color.red;
        }
        else if (m_isTouched)
        {
            statusImage.color = Color.yellow;
        }
        else
        {
            statusImage.color = Color.white;
        }
    }
}
