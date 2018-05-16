using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;

public class Gamepad4InputSystem : MonoBehaviour {

    public InputField unmapped_button_list;

    // private List<string> control_pathes = new List<string>();
    private InputAction button_press_action;    
    private InputAction dpad_press_action;
    private InputAction stick_move_action;

    // Use this for initialization
    void Start () {
        button_press_action = new InputAction(name: "ButtonPressAction", binding: "*/<button>");
        button_press_action.performed += callbackContext => ButtonPress(callbackContext.control as ButtonControl);
        button_press_action.Enable();

        dpad_press_action = new InputAction(name: "DpadPressAction", binding: "*/<dpad>");
        dpad_press_action.performed += callbackContext => DpadPress(callbackContext.control as DpadControl);
        dpad_press_action.Enable();

        stick_move_action = new InputAction(name: "StickMoveAction", binding: "*/<stick>");
        stick_move_action.performed += callbackContext => StickMove(callbackContext.control as StickControl);
        stick_move_action.Enable();
    }

    private void DpadPress(DpadControl control)
    {
        Transform dpad = GetInputTransform(control.name, isDpad:true);
        DpadButtonPress(control.up, dpad);
        DpadButtonPress(control.down, dpad);
        DpadButtonPress(control.left, dpad);
        DpadButtonPress(control.right, dpad);
    }

    private void DpadButtonPress(ButtonControl control, Transform dpad)
    {
        Transform button = dpad.Find(control.name);
        if (control.ReadValue() > 0)
            StartHighlight(button);
        else
            StopHighlight(button);
    }

    private void ButtonPress(ButtonControl control)
    {
        // Rule out Keyboard and Mouse input
        string device = control.device.description.deviceClass;
        if (device == "Keyboard" || device == "Mouse")
            return;
        
        // If the button input is from pressing a stick
        bool isStick = control.name.Contains("StickPress") ? true : false;
        string buttonName = isStick ? control.name.Replace("Press", "") : control.name.Replace("button", "");
        Transform button = GetInputTransform(buttonName, isStick);
        
        if (button == null) return;

        // For stick, the highlighted part is the moving part
        if (isStick) button = button.Find("Stick");

        if (control.ReadValue() > 0)
            StartHighlight(button);
        else
            StopHighlight(button);            
    }

    private void StickMove(StickControl control)
    {
        Transform stick = GetInputTransform(control.name, isStick: true);
        stick = stick.Find("Stick");
        Vector2 pos = control.ReadValue() * 0.5f;
        stick.localPosition = new Vector3(pos.x, pos.y, -0.01f);
    }

    // Get the Transform in scene for input control (button, stick, dpad)
    // If no existing one is assigned, assign a new one
    private Transform GetInputTransform(string inputName, bool isStick = false, bool isDpad = false)
    {
        Transform input = transform.Find("Input System/" + inputName);
        // First time use
        if (input == null)
        {           
            if (isStick)        input = transform.Find("Input System/Gamepad_Stick");
            else if (isDpad)    input = transform.Find("Input System/Gamepad_Dpad");
            else                input = transform.Find("Input System/Gamepad_Button");            

            // if unassigned Gameobject ran out. highly unlikely, but in case
            if (input == null)
                AddUnmappedButton(inputName);
            else
            {
                input.name = inputName;
                FirstTimeUse(input);
            }            
        }
        return input;
    }
      
    // When a input is used for the first time, remove all tranparency from it
    private void FirstTimeUse(Transform controlTrans)
    {
        // Remove transparency from all the Sprite Renderers
        foreach (SpriteRenderer sr in controlTrans.GetComponentsInChildren<SpriteRenderer>())
            sr.color = RemoveColorTranparency(sr.color);

        // Remove transparency from the text mesh and change text to the transform's name
        TextMesh tm = controlTrans.GetComponentInChildren<TextMesh>();
        tm.color = RemoveColorTranparency(tm.color);
        tm.text = controlTrans.name;
            
    }

    private void StartHighlight(Transform controlTrans)
    {
        SpriteRenderer sr = controlTrans.Find("Highlight_Input_System").GetComponent<SpriteRenderer>();
        sr.enabled = true;
    }

    private void StopHighlight(Transform controlTrans)
    {
        SpriteRenderer sr = controlTrans.Find("Highlight_Input_System").GetComponent<SpriteRenderer>();
        sr.enabled = false;
    }

    private Color RemoveColorTranparency(Color color)
    {
        color.a = 1f;
        return color;
    }

    private void AddUnmappedButton(string buttonName)
    {
        unmapped_button_list.text += "<color=red>" + buttonName + "</color>\n";
    }
}
