using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;

public class Xbox4InputSystem : MonoBehaviour
{
    // public InputField unmapped_key_list;
    private InputAction button_press_action;
    private InputAction dpad_press_action;

    private AnalogStick[] analog_sticks = new AnalogStick[2];

    // Use this for initialization
    void Start()
    {
        button_press_action = new InputAction(name: "XboxButtonAction", binding: "/XInputController*/<button>");
        button_press_action.performed += callbackContext => ButtonPress(callbackContext.control as ButtonControl);
        button_press_action.Enable();

        dpad_press_action = new InputAction(name: "XboxDpadAction", binding: "/XInputController*/<dpad>");
        dpad_press_action.performed += callbackContext => DpadPress(callbackContext.control as DpadControl);
        dpad_press_action.Enable();

        analog_sticks[0] = new AnalogStick(transform.Find("Buttons/LeftStick"));
        analog_sticks[1] = new AnalogStick(transform.Find("Buttons/RightStick"));
    }

    // Update is called once per frame
    void Update()
    {
        Gamepad xController = Gamepad.current;
        if (xController != null)
        {
            analog_sticks[0].UpdatePosition(xController.leftStick.ReadValue());
            analog_sticks[1].UpdatePosition(xController.rightStick.ReadValue());
            UpdateStick(xController.leftStick);
            UpdateStick(xController.rightStick);
        }
    }

    private void UpdateStick(StickControl control)
    {
        string stickName = FirstLetterToUpper(control.name);
        TextMesh textMesh = transform.Find("Buttons/" + stickName).GetComponentInChildren<TextMesh>();
        if (textMesh != null)
            textMesh.text = control.ReadValue().ToString();
    }

    // callback funtion when any button on dpad is pressed
    private void DpadPress(DpadControl control)
    {
        ButtonPress(control.up, true);
        ButtonPress(control.down, true);
        ButtonPress(control.left, true);
        ButtonPress(control.right, true);
    }

    // callback function when any button on the controller is pressed, except dpad
    private void ButtonPress(ButtonControl control, bool isDpad = false)
    {
        string buttonName = control.name;
        if (buttonName.Contains("button"))
            buttonName = control.aliases[0];
        else if (buttonName.Contains("Press"))
            buttonName = buttonName.Replace("Press", "");
        buttonName = isDpad ? "Dpad" + FirstLetterToUpper(buttonName) : FirstLetterToUpper(buttonName);

        if (control.ReadValue() > 0)
            StartHighlight(buttonName);
        else
            StopHighlight(buttonName);
    }

    private void StartHighlight(string buttonName)
    {
        SpriteRenderer highlight = GetHighlightComponent(buttonName);
        if (highlight != null)
            highlight.enabled = true;
    }

    private void StopHighlight(string buttonName)
    {
        SpriteRenderer highlight = GetHighlightComponent(buttonName);
        if (highlight != null)
            highlight.enabled = false;
    }

    private SpriteRenderer GetHighlightComponent(string buttonName)
    {
        Transform button = transform.Find("Buttons/" + buttonName + "/Highlight_Input_System");
        if (button == null)
            return null;

        //Transform hl = button.Find("Highlight_Input_System");
        //if (hl == null)
        //    return null;
        return button.GetComponent<SpriteRenderer>();
    }

    private string FirstLetterToUpper(string str)
    {
        if (String.IsNullOrEmpty(str))
            return null;
        else if (str.Length == 1)
            return str.ToUpper();
        else
            return char.ToUpper(str[0]) + str.Substring(1);
    }
}
