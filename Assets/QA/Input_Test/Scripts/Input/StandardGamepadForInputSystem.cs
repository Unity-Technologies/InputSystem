using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;

//---------------------------------------------------------------------------
// Parent Class for All Gamepad/Controller Input from New Input System.
//---------------------------------------------------------------------------

public class StandardGamepadForInputSystem : MonoBehaviour
{
    [Tooltip("The GameObject that is the parent for all the buttons.")]
    public Transform buttons_container;
    public InputField unmapped_button_list;

    protected InputAction button_press_action;
    protected InputAction dpad_press_action;
    protected InputAction stick_move_action;

    // Callback funtion when a button in a dpad is pressed.
    protected virtual void OnDpadPress(DpadControl control)
    {
        string dpadName = FirstLetterToUpper(control.name);
        OnControllerBUttonPress(control.up, dpadName);
        OnControllerBUttonPress(control.down, dpadName);
        OnControllerBUttonPress(control.left, dpadName);
        OnControllerBUttonPress(control.right, dpadName);
    }

    // Callback function when a stick is moved.
    protected virtual void StickMove(StickControl control)
    {
        Transform stick = GetInputTransform(FirstLetterToUpper(control.name), isStick: true);
        Vector2 pos = control.ReadValue() * 0.5f;
        stick.localPosition = new Vector3(pos.x, pos.y, -0.01f);
    }

    // If the one of the controller button is pressed
    protected virtual void OnControllerBUttonPress(ButtonControl control, string dpadName = null, bool isXbox = false, bool isPS = false)
    {
        string buttonName = control.name;
        Transform button = null;

        // If the button input is from pressing a stick
        if (buttonName.Contains("StickPress"))
        {
            buttonName = buttonName.Replace("Press", "");
            button = GetInputTransform(FirstLetterToUpper(buttonName), isStick: true);
        }
        else
        {
            if (control.aliases.Count > 0)
            {
                if (isXbox)    buttonName = control.aliases[0];
                else if (isPS) buttonName = control.aliases[1];
                else           buttonName = control.name.Replace("button", "");
            }
            button = GetInputTransform(FirstLetterToUpper(buttonName), dpadName: dpadName);
        }

        if (control.ReadValue() > 0)
            StartHighlight(button);
        else
            StopHighlight(button);
    }

    // Find a transform for a input.
    // isDpad:
    // isStick: Used when stick is moved or pressed. Find the child transform named "stick"
    protected virtual Transform GetInputTransform(string inputName, bool isStick = false, string dpadName = null)
    {
        Transform input;
        if (isStick)               input = buttons_container.Find(inputName + "/Stick");
        else if (dpadName != null) input = buttons_container.Find(dpadName + "/" + inputName);
        else                       input = buttons_container.Find(inputName);

        // The transform does not exist for the input button
        if (input == null)
            AddUnmappedButton(inputName);

        return input;
    }

    protected void StartHighlight(Transform controlTrans)
    {
        SpriteRenderer sr = controlTrans.Find("Highlight_Input_System").GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = true;
    }

    protected void StopHighlight(Transform controlTrans)
    {
        SpriteRenderer sr = controlTrans.Find("Highlight_Input_System").GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = false;
    }

    protected void AddUnmappedButton(string buttonName)
    {
        unmapped_button_list.text += "<color=red>" + buttonName + "</color>\n";
    }

    protected string FirstLetterToUpper(string str)
    {
        if (String.IsNullOrEmpty(str))
            return null;
        else if (str.Length == 1)
            return str.ToUpper();
        else
            return char.ToUpper(str[0]) + str.Substring(1);
    }
}
