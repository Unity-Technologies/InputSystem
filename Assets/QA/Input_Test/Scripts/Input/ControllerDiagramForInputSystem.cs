using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;

public class ControllerDiagramForInputSystem : StandardGamepadForInputSystem
{
    // Use this for initialization
    void Start()
    {
        button_press_action = new InputAction(name: "ButtonPressAction", binding: "*/<button>");
        button_press_action.performed += callbackContext => OnButtonPress(callbackContext.control as ButtonControl);
        button_press_action.Enable();

        dpad_press_action = new InputAction(name: "dpadpressaction", binding: "*/<dpad>");
        dpad_press_action.performed += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        dpad_press_action.Enable();

        stick_move_action = new InputAction(name: "StickMoveAction", binding: "*/<stick>");
        stick_move_action.performed += callbackContext => StickMove(callbackContext.control as StickControl);
        stick_move_action.Enable();
    }

    // Callback funtion when a button is pressed. The button can be on a keyboard or mouse
    private void OnButtonPress(ButtonControl control)
    {
        // Rule out Keyboard and Mouse input
        string device = control.device.description.deviceClass;
        if (device == "Keyboard" || device == "Mouse")
            return;

        OnControllerBUttonPress(control);
    }

    // Get the Transform in scene for input control (button, stick, dpad)
    // If no existing one is assigned, assign a new one
    protected override Transform GetInputTransform(string inputName, bool isStick = false, string dpadName = null)
    {
        bool isDpad = (dpadName == null) ? false : true;
        Transform input = isDpad ? m_buttonContainer.Find(dpadName) : m_buttonContainer.Find(inputName);
        // First time use
        if (input == null)
        {
            if (isStick)     input = m_buttonContainer.Find("Gamepad_Stick");
            else if (isDpad) input = m_buttonContainer.Find("Gamepad_Dpad");
            else             input = m_buttonContainer.Find("Gamepad_Button");

            // if unassigned Gameobject ran out. highly unlikely, but in case
            if (input == null)
            {
                ShowMessage(inputName);
                return null;
            }
            else
            {
                input.name = isDpad ? dpadName : inputName;
                FirstTimeUse(input);
            }
        }
        if (isStick) input = input.Find("Stick");
        else if (isDpad) input = input.Find(inputName);

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

    private Color RemoveColorTranparency(Color color)
    {
        color.a = 1f;
        return color;
    }
}
