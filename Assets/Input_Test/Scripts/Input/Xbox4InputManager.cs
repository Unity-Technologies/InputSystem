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

public class Xbox4InputManager : MonoBehaviour
{

    public InputField unmapped_button_list;
    public ParticleSystem highlight_input_manager;

    private Dictionary<string, string> button_map = new Dictionary<string, string>();
    private List<AnalogStick> analog_sticks = new List<AnalogStick>();
    private List<AnalogButton> analog_buttons = new List<AnalogButton>();
    private List<XboxTrigger> xbox_triggers = new List<XboxTrigger>();

    // Use this for initialization
    void Start () {
        // Button map is different for each platform
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        button_map.Add("Button0", "A");
        button_map.Add("Button1", "B");
        button_map.Add("Button2", "X");
        button_map.Add("Button3", "Y");
        button_map.Add("Button4", "LeftShoulder");
        button_map.Add("Button5", "RightShoulder");
        button_map.Add("Button6", "Select");
        button_map.Add("Button7", "Start");
        button_map.Add("Button8", "LeftStick");
        button_map.Add("Button9", "RightStick");
        analog_sticks.Add(new AnalogStick(transform.Find("Buttons/LeftStick"), "Axis 1", "Axis 2", isYReversed: true));
        analog_sticks.Add(new AnalogStick(transform.Find("Buttons/RightStick"), "Axis 4", "Axis 5", isYReversed: true));
        analog_buttons.Add(new AnalogButton(transform.Find("Buttons/LeftTrigger"), "Axis 3", -1f, 0f));
        analog_buttons.Add(new AnalogButton(transform.Find("Buttons/RightTrigger"), "Axis 3", 0f, 1f));
        analog_buttons.Add(new AnalogButton(transform.Find("Buttons/DpadLeft"), "Axis 6", -1f, 0f));
        analog_buttons.Add(new AnalogButton(transform.Find("Buttons/DpadRight"), "Axis 6", 0f, 1f));
        analog_buttons.Add(new AnalogButton(transform.Find("Buttons/DpadUp"), "Axis 7", 0f, 1f));
        analog_buttons.Add(new AnalogButton(transform.Find("Buttons/DpadDown"), "Axis 7", -1f, 0f));

#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        button_map.Add("Button16", "A");
        button_map.Add("Button17", "B");
        button_map.Add("Button18", "X");
        button_map.Add("Button19", "Y");
        button_map.Add("Button13", "LeftShoulder");
        button_map.Add("Button14", "RightShoulder");
        button_map.Add("Button10", "Select");
        button_map.Add("Button9", "Start");
        button_map.Add("Button11", "LeftStick");
        button_map.Add("Button12", "RightStick");
        button_map.Add("Button5", "DpadUp");
        button_map.Add("Button6", "DpadDown");
        button_map.Add("Button7", "DpadLeft");
        button_map.Add("Button8", "DpadRight");
        button_map.Add("Button15", "Xbox_Button");
        analog_sticks.Add(new AnalogStick(transform.Find("Buttons/LeftStick"), "Axis 1", "Axis 2", 0.5f));
        analog_sticks.Add(new AnalogStick(transform.Find("Buttons/RightStick"), "Axis 3", "Axis 4", 0.5f));
        xbox_triggers.Add(new XboxTrigger(transform.Find("Buttons/LeftTrigger"), "Axis 5"));
        xbox_triggers.Add(new XboxTrigger(transform.Find("Buttons/RightTrigger"), "Axis 6"));
#endif
    }

    // Update is called once per frame
    void Update () {
        // When a joystick button is pressed
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
                StartHighlightButton(kcode);
            if (Input.GetKeyUp(kcode))
                StopHighlightButton(kcode);
        }

        // Update all analog keys
        foreach (AnalogButton button in analog_buttons)
        {
            float inputValue = Input.GetAxis(button.Axis_Name);
            if (button.IsPressed(inputValue))
                StartHighlightButton(button.Name);
            else
                StopHighlightButton(button.Name);
        }

        foreach (XboxTrigger trigger in xbox_triggers)
        {
            float inputValue = Input.GetAxis(trigger.Axis_Name);
            if (trigger.IsTriggered(inputValue))
                StartHighlightButton(trigger.Name);
            else
                StopHighlightButton(trigger.Name);
        }

        // Update all analog sticks
        foreach (AnalogStick stick in analog_sticks)
        {
            float xInput = Input.GetAxis(stick.X_Axis_Name);
            float yInput = Input.GetAxis(stick.Y_Axis_Name);
            stick.UpdatePosition(xInput, yInput);
        }
    }

    private void StartHighlightButton(KeyCode kcode)
    {
        string buttonCode = GetButtonCode(kcode);
        if (buttonCode == null)
            return;

        string buttonName = GetButtonName(buttonCode);
        if (buttonName != null)
        {
            StartHighlightButton(buttonName);
            Debug.Log(buttonName + " down");
        }            
        else
            AddUnmappedButton(buttonCode);
    }

    private void StopHighlightButton(KeyCode kcode)
    {
        string buttonCode = GetButtonCode(kcode);
        if (buttonCode == null)
            return;

        string buttonName = GetButtonName(buttonCode);
        if (buttonName != null)
        {
            StopHighlightButton(buttonName);
            Debug.Log(buttonName + " up");
        }            
    }

    private void StartHighlightButton(string buttonName)
    {
        Transform button = transform.Find("Buttons/" + buttonName);        
        ParticleSystem ps = button.GetComponentInChildren<ParticleSystem>();
        if (ps == null)
            Instantiate(highlight_input_manager, button.position - new Vector3(0f, 0f, 0.1f), button.rotation, button);
        else
            ps.Play();
    }

    private void StopHighlightButton(string buttonName)
    {
        Transform button = transform.Find("Buttons/" + buttonName);        
        ParticleSystem[] ps = button.GetComponentsInChildren<ParticleSystem>();
        if (ps.Length > 0)
        {
            foreach (ParticleSystem p in ps)
                p.Stop();
        }
    }

    // Remove "Joystick" from the key code value to find the button through name
    private string GetButtonCode(KeyCode kcode)
    {
        string kcodeString = kcode.ToString();
        if (kcodeString.Contains("JoystickButton"))
            return kcodeString.Replace("Joystick", "");
        else
            return null;
    }

    private string GetButtonName(string buttonCode)
    {
        string buttonName = null;
        button_map.TryGetValue(buttonCode, out buttonName);
        return buttonName;
    }

    private void AddUnmappedButton(string buttonName)
    {
        unmapped_button_list.text += "<color=blue>" + buttonName + "</color>\n";
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

    public string Name { get { return name; } }
    public string Axis_Name { get { return axis_name; } }
    public float Deadzone { get { return deadzone; } }
    public Transform Button
    {
        get { return button; }
        set
        {
            button = value;
            name = button.name;
        }
    }

    public AnalogButton(Transform btn, string axisName)
    {
        Button = btn;
        axis_name = axisName;
    }

    public AnalogButton(Transform btn, string axisName, float minValue, float maxValue)
    {
        Button = btn;
        axis_name = axisName;
        min_input_value = minValue;
        max_input_value = maxValue;
    }

    public AnalogButton(Transform btn, string axisName, float minValue, float maxValue, float deadzn)
    {
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

// This is for xbox controller triggers on MacOS ONLY
// The trigger initially starts at 0 until it is first used. Then the range is [-1, 1].
public class XboxTrigger : AnalogButton
{
    private bool is_first = true;

    public XboxTrigger(Transform trigger, string axisName) : base(trigger, axisName) {}

    public bool IsTriggered(float inputValue)
    {
        if (is_first)
        {
            is_first = false;
            return IsPressed(inputValue);
        }
        else
        {
            if (inputValue > deadzone)
                return true;
            else
                return false;
        }
    }
}
