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

public class StandardGamepadForInputManager : MonoBehaviour
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

    protected void StartHighlightButton(string buttonName)
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

    protected void StopHighlightButton(string buttonName)
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
    public bool IsPressed(float inputValue)
    {
        if (Mathf.Abs(inputValue) > deadzone && inputValue >= min_input_value && inputValue <= max_input_value)
            return true;
        else
            return false;
    }
}
