using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;

public class XboxForInputSystem : StandardGamepadForInputSystem
{
    // Use this for initialization
    void Start()
    {
        button_press_action = new InputAction(name: "XboxButtonAction", binding: "/XInputController*/<button>");
        button_press_action.performed += callbackContext => OnControllerBUttonPress(callbackContext.control as ButtonControl, isXbox: true);
        button_press_action.Enable();

        dpad_press_action = new InputAction(name: "XboxDpadAction", binding: "/XInputController*/<dpad>");
        dpad_press_action.performed += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        dpad_press_action.Enable();

        stick_move_action = new InputAction(name: "StickMoveAction", binding: "XInputController*/<stick>");
        stick_move_action.performed += callbackContext => StickMove(callbackContext.control as StickControl);
        stick_move_action.Enable();

        //analog_sticks[0] = new AnalogStick(transform.Find("Buttons/LeftStick"));
        //analog_sticks[1] = new AnalogStick(transform.Find("Buttons/RightStick"));
    }

    //protected override void StickMove(StickControl control)
    //{
    //    Transform stick = GetInputTransform(FirstLetterToUpper(control.name), isStick: true);
    //    TextMesh textMesh = stick.Find("Value_Input_System").GetComponent<TextMesh>();
    //    if (textMesh != null)
    //        textMesh.text = control.ReadValue().ToString();
    //}
}
