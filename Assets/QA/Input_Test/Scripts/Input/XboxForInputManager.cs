using System.Collections.Generic;
using UnityEngine;

// ------------------------- Setup Requirement ------------------------------------
// In Input Manager, the nth axis on joystick should be named as "Axis n"
//   For Example: X anis is named "Axis 1" and 4th axis is named "Axis 4"
// All axes' Sensitivity should be set to 1. (Default is 0.1)
// Alternatively, ingore the requirement and change the code
// --------------------------------------------------------------------------------

public class XboxForInputManager : StandardGamepadForInputManager
{
    private List<XboxTrigger> xbox_triggers = new List<XboxTrigger>();

    // Use this for initialization
    void Start()
    {
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
        button_map.Add("Button8", "LeftStick/Stick");
        button_map.Add("Button9", "RightStick/Stick");
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("LeftStick"), "Axis 1", "Axis 2", isYReversed: true));
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("RightStick"), "Axis 4", "Axis 5", isYReversed: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("LeftTrigger"), "Axis 3", -1f, 0f));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("RightTrigger"), "Axis 3", 0f, 1f));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Left"), "Axis 6", -1f, 0f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Right"), "Axis 6", 0f, 1f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Up"), "Axis 7", 0f, 1f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Down"), "Axis 7", -1f, 0f, isDpad: true));

#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        button_map.Add("Button16", "A");
        button_map.Add("Button17", "B");
        button_map.Add("Button18", "X");
        button_map.Add("Button19", "Y");
        button_map.Add("Button13", "LeftShoulder");
        button_map.Add("Button14", "RightShoulder");
        button_map.Add("Button10", "Select");
        button_map.Add("Button9", "Start");
        button_map.Add("Button11", "LeftStick/Stick");
        button_map.Add("Button12", "RightStick/Stick");
        button_map.Add("Button5", "Dpad/Up");
        button_map.Add("Button6", "Dpad/Down");
        button_map.Add("Button7", "Dpad/Left");
        button_map.Add("Button8", "Dpad/Right");
        button_map.Add("Button15", "Xbox");
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("LeftStick"), "Axis 1", "Axis 2", isYReversed: true));
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("RightStick"), "Axis 3", "Axis 4", isYReversed: true));
        xbox_triggers.Add(new XboxTrigger(m_buttonContainer.Find("LeftTrigger"), "Axis 5"));
        xbox_triggers.Add(new XboxTrigger(m_buttonContainer.Find("RightTrigger"), "Axis 6"));
#endif
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAllButtons();
        UpdateAllAnalogSticks();
        UpdateAllAnalogButtons();

        // XboxTrigger is only used in MacOS
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        foreach (XboxTrigger trigger in xbox_triggers)
        {
            float inputValue = Input.GetAxis(trigger.Axis_Name);
            if (trigger.IsTriggered(inputValue))
                StartHighlightButton(trigger.Name);
            else
                StopHighlightButton(trigger.Name);
        }
#endif
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
