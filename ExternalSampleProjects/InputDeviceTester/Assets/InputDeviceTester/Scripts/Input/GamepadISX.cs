using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

//---------------------------------------------------------------------------
// Parent Class for All Gamepad/Controller Input from New Input System.
//---------------------------------------------------------------------------

public class GamepadISX : MonoBehaviour
{
    [Tooltip("The GameObject that is the parent for all the buttons.")]
    public Transform m_buttonContainer;

    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    protected float m_stickMaxMove = 0.5f;    // The range for stick gameobject movement

    protected InputAction m_buttonAction;
    protected InputAction m_dPadAction;
    protected InputAction m_stickMoveAction;

    // Callback funtion when a button in a dpad is pressed.
    protected virtual void OnDpadPress(DpadControl control)
    {
        string dpadName = FirstLetterToUpper(control.name);
        OnControllerButtonPress(control.up, dpadName);
        OnControllerButtonPress(control.down, dpadName);
        OnControllerButtonPress(control.left, dpadName);
        OnControllerButtonPress(control.right, dpadName);
    }

    // Callback function when a stick is moved.
    protected virtual void StickMove(StickControl control)
    {
        Vector2 pos = control.ReadValue();
        Transform stick = GetInputTransform(FirstLetterToUpper(control.name), isStick: true);
        if (stick != null)
            stick.localPosition = new Vector3(pos.x * m_stickMaxMove, pos.y * m_stickMaxMove, stick.localPosition.z);
    }

    // If the one of the controller button is pressed
    protected virtual void OnControllerButtonPress(ButtonControl control, string dpadName = null, bool isXbox = false, bool isPS = false)
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

        if (button == null)
            return;

        if (control.ReadValue() > 0)
            StartHighlight(button);
        else
            StopHighlight(button);
    }

    // Find a transform for a input.
    // dpadName: to find the transform. Then find child transfomr with the same name from control
    // isStick: Used when stick is moved or pressed. Find the child transform named "stick"
    protected virtual Transform GetInputTransform(string inputName, bool isStick = false, string dpadName = null)
    {
        Transform input;
        if (isStick)               input = m_buttonContainer.Find(inputName + "/Stick - Input System");
        else if (dpadName != null) input = m_buttonContainer.Find(dpadName + "/" + inputName);
        else                       input = m_buttonContainer.Find(inputName);

        // The transform does not exist for the input button
        if (input == null)
            ShowMessage(inputName);

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

    protected string FirstLetterToUpper(string str)
    {
        if (String.IsNullOrEmpty(str))
            return null;
        else if (str.Length == 1)
            return str.ToUpper();
        else
            return char.ToUpper(str[0]) + str.Substring(1);
    }

    protected void ShowMessage(string msg)
    {
        m_MessageWindow.text += "<color=red>" + msg + "</color>\n";
    }
}
