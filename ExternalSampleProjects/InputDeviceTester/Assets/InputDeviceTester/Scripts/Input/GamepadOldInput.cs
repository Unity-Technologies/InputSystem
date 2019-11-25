using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ------------------------- Setup Requirement ------------------------------------
// In Input Manager, the nth axis on joystick should be named as "Axis n"
//   For Example: X anis is named "Axis 1" and 4th axis is named "Axis 4"
// All axes' Sensitivity should be set to 1. (Default is 0.1)
// Alternatively, ingore the requirement and change the code
// --------------------------------------------------------------------------------

public class GamepadOldInput : MonoBehaviour
{
    [Tooltip("Highlight Prefab")]
    public ParticleSystem m_buttonHighlight;

    [Tooltip("The GameObject that is the parent for all the buttons.")]
    public Transform m_buttonContainer;

    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    protected Dictionary<string, string> button_map = new Dictionary<string, string>();
    protected List<AnalogStick> analog_sticks = new List<AnalogStick>();
    protected List<AnalogButton> analog_buttons = new List<AnalogButton>();

#if ENABLE_LEGACY_INPUT_MANAGER
    protected void UpdateAllButtons()
    {
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
                StartHighlightButton(kcode);
            if (Input.GetKeyUp(kcode))
                StopHighlightButton(kcode);
        }
    }

    protected virtual void UpdateAllAnalogSticks()
    {
        foreach (AnalogStick stick in analog_sticks)
        {
            float xInput = Input.GetAxis(stick.X_Axis_Name);
            float yInput = Input.GetAxis(stick.Y_Axis_Name);
            stick.UpdatePosition(xInput, yInput);
        }
    }

    protected void UpdateAllAnalogButtons()
    {
        foreach (AnalogButton button in analog_buttons)
        {
            float inputValue = Input.GetAxis(button.Axis_Name);
            if (button.IsPressed(inputValue))
                StartHighlightButton(button.Name);
            else
                StopHighlightButton(button.Name);
        }
    }

    protected void StartHighlightButton(KeyCode kcode)
    {
        string buttonCode = GetButtonCode(kcode);
        if (buttonCode != null)
        {
            string buttonName = GetButtonName(buttonCode);
            if (buttonName != null)
            {
                StartHighlightButton(buttonName);
                //Debug.Log(buttonName + " down");
            }
            else
                ShowMessage(buttonCode);
        }
    }

    protected void StopHighlightButton(KeyCode kcode)
    {
        string buttonCode = GetButtonCode(kcode);
        if (buttonCode != null)
        {
            string buttonName = GetButtonName(buttonCode);
            if (buttonName != null)
            {
                StopHighlightButton(buttonName);
                //Debug.Log(buttonName + " up");
            }
        }
    }

    protected virtual void StartHighlightButton(string buttonName)
    {
        Transform button = m_buttonContainer.Find(buttonName);
        if (button == null)
            ShowMessage(buttonName);
        else
        {
            ParticleSystem ps = button.GetComponentInChildren<ParticleSystem>();
            if (ps == null)
                Instantiate(m_buttonHighlight, button.position - new Vector3(0f, 0f, 0.1f), button.rotation, button);
            else
                ps.Play();
        }
    }

    protected virtual void StopHighlightButton(string buttonName)
    {
        Transform button = m_buttonContainer.Find(buttonName);
        if (button != null)
        {
            ParticleSystem[] ps = button.GetComponentsInChildren<ParticleSystem>();
            if (ps.Length > 0)
            {
                foreach (ParticleSystem p in ps)
                    p.Stop();
            }
        }
    }

    // Remove "Joystick" from the key code value to find the button through code name
    protected string GetButtonCode(KeyCode kcode)
    {
        string kcodeString = kcode.ToString();
        if (kcodeString.Contains("JoystickButton"))
            return kcodeString.Replace("Joystick", "");
        else
            return null;
    }

    // If the button has a name, like button0 on xbox controller is called A in Windows environment.
    protected string GetButtonName(string buttonCode)
    {
        string buttonName;
        if (button_map.TryGetValue(buttonCode, out buttonName))
            return buttonName;
        else
            return buttonCode;
    }

    protected void ShowMessage(string msg)
    {
        m_MessageWindow.text += "<color=blue>" + msg + "</color>\n";
    }

#endif
}

[Serializable]
public class AnalogStick
{
    //-------------------------------------------------------------------------------------------
    // For Input Manager:
    // Each Analog Stck has 2 different axis associated with it
    // One is control by the stick's movement in X direction; the other is controled by Y.
    //-------------------------------------------------------------------------------------------

    private Transform stick;            // The moving part of the analog stick. It is the child object named "Stick";
    private string name;                // The name for the Transform Stick. It should make sense for the controller used, such as "Left_Stick" for Xbox controller.
    private string x_axis_name;         // The Axis controlled through the stick's movement in X direction. The name is set in Input Manager.
    private string y_axis_name;         // The Axis controlled through the stick's movement in Y direction. The name is set in Input Manager.
    private bool is_y_reversed = false; // In case the Y axis is reversed, like for Input Manager.
    private bool is_x_reversed = false; // In case the X axis is reversed. Probably not useful.

    private float max_move_distance;    // The distance of the transform can move in each direction
    private Vector3 original_position;  // The stick's initial position in the scene

    private Text m_positionText;        // The UI Text to show the stick's position. It is optional.

    public string Name { get { return name; } }
    public string X_Axis_Name { get { return x_axis_name; } }
    public string Y_Axis_Name { get { return y_axis_name; } }
    public float Max_Move_Distance { get { return max_move_distance; } }
    public Transform Stick
    {
        get { return stick; }
        set
        {
            name = value.parent.name;
            stick = value;
            original_position = stick.position;
        }
    }

    // For Input Manager Initialization
    public AnalogStick(Transform stck, string XName, string YName, Text posText = null, float maxDistance = 0.5f, bool isYReversed = false)
    {
        x_axis_name = XName;
        y_axis_name = YName;
        max_move_distance = maxDistance;
        Stick = stck;
        is_y_reversed = isYReversed;
        m_positionText = posText;
    }

    // Update the stick position according to the input value
    public void UpdatePosition(float xValue, float yValue)
    {
        if (m_positionText != null)
            m_positionText.text = "(" + xValue.ToString("F2") + ", " + yValue.ToString("F2") + ")";

        if (is_x_reversed) xValue *= -1;
        if (is_y_reversed) yValue *= -1;
        Vector3 adjust = new Vector3(xValue * max_move_distance, yValue * max_move_distance, 0f);
        stick.position = original_position + adjust;
    }

    public void UpdatePosition(Vector2 pos)
    {
        UpdatePosition(pos.x, pos.y);
    }
}

[Serializable]
public class AnalogButton
{
    protected Transform button;       // The Transform for the button
    protected string name;            // Name of the Transform. It should make sense for the context, such as "Left_Trigger" for xbox controller.
    protected string axis_name;       // The name of the axis it associated with. This is set in Input Manager.

    // It may take a pair of buttons to complete the whole axis.
    // For example: D-Pad on Windows are represented as Axis 6 and 7.
    // In that case, DPad_Left input value range is [-1, 0] while DPad_Right input value range is (0, 1]
    protected float min_input_value = -1;
    protected float max_input_value = 1;
    protected float deadzone = 0.1f;
    protected bool is_dpad = false;

    public string Name { get { return name; } }
    public string Axis_Name { get { return axis_name; } }
    public float Deadzone { get { return deadzone; } }
    public Transform Button
    {
        get { return button; }
        set
        {
            button = value;
            name = is_dpad ? button.parent.name + "/" + button.name : button.name;
        }
    }

    public AnalogButton(Transform btn, string axisName, float minValue = -1, float maxValue = 1, float deadzn = 0.1f, bool isDpad = false)
    {
        is_dpad = isDpad;
        Button = btn;
        axis_name = axisName;
        min_input_value = minValue;
        max_input_value = maxValue;
        deadzone = deadzn;
    }

    // Decide if the button is "pressed" based on the input value
    public virtual bool IsPressed(float inputValue)
    {
        if (Mathf.Abs(inputValue) > deadzone && inputValue >= min_input_value && inputValue <= max_input_value)
            return true;
        else
            return false;
    }
}
