using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.UI;

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
            ButtonControl control = controls[i] as ButtonControl;
            if (control != null)
                m_isTouched = control.isPressed;
            else
            {
                Debug.LogWarningFormat(this, "ButtonControlActionStatus expects bindings of type {1}, but found {1} binding named {2}.", typeof(ButtonControl).FullName, controls[i].GetType().FullName, controls[i].name);
            }
        }

        buttonPressAction.performed += UpdatePressStatus;
        buttonPressAction.started += UpdatePressStatus;
        buttonPressAction.cancelled += UpdatePressStatus;
        buttonPressAction.Enable();

        controls = buttonPressAction.controls;
        for (int i = 0; i < controls.Count; i++)
        {
            ButtonControl control = controls[i] as ButtonControl;
            if (control != null)
                m_isPressed = control.isPressed;
            else
            {
                Debug.LogWarningFormat(this, "ButtonControlActionStatus expects bindings of type {1}, but found {1} binding named {2}.", typeof(ButtonControl).FullName, controls[i].GetType().FullName, controls[i].name);
            }
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
        ButtonControl control = context.control as ButtonControl;
        if (control != null)
        {
            m_isPressed = control.isPressed;
            ApplyStatusColor();
        }
    }

    private void UpdateTouchStatus(InputAction.CallbackContext context)
    {
        ButtonControl buttonControl = context.control as ButtonControl;
        if (buttonControl != null)
        {
            m_isTouched = buttonControl.isPressed;
            ApplyStatusColor();
        }
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
