using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ------------------------- Setup Requirement ------------------------------------
// In Input Manager, the nth axis on joystick should be named as "Axis n"
//   For Example: X anis is named "Axis 1" and 4th axis is named "Axis 4"
// All axes' Sensitivity should be set to 1. (Default is 0.1)
// Alternatively, ingore the requirement and change the code
// --------------------------------------------------------------------------------

public class DualShockOldInput : GamepadOldInput
{
    [Header("UI Element for Other Information")]
    public Text m_leftStickText;
    public Text m_rightStickText;

    private List<DualShockTrigger> m_dualShockTriggers = new List<DualShockTrigger>();
    private readonly Color m_stickButtonColor = new Color(0.4f, 0.4f, 0.55f, 1f);    // The default color for Stick when it is NOT pressed.

#if ENABLE_LEGACY_INPUT_MANAGER
    // Start is called before the first frame update
    void Start()
    {
        // Button map is different for each platform
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
        button_map.Add("Button0", "Square");
        button_map.Add("Button1", "Cross");
        button_map.Add("Button2", "Circle");
        button_map.Add("Button3", "Triangle");
        button_map.Add("Button4", "LeftShoulder");
        button_map.Add("Button5", "RightShoulder");
        //button_map.Add("Button6", "LeftTrigger");
        //button_map.Add("Button7", "RightTrigger");
        button_map.Add("Button8", "Select");
        button_map.Add("Button9", "Start");
        button_map.Add("Button10", "LeftStick/Stick - Input Manager");
        button_map.Add("Button11", "RightStick/Stick - Input Manager");
        button_map.Add("Button12", "SystemButton");
        button_map.Add("Button13", "TouchpadButton");
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("LeftStick/Stick - Input Manager"), "Axis 1", "Axis 2", posText: m_leftStickText, isYReversed: true));
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("RightStick/Stick - Input Manager"), "Axis 3", "Axis 6", posText: m_rightStickText, isYReversed: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Left"), "Axis 7", -1f, 0f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Right"), "Axis 7", 0f, 1f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Up"), "Axis 8", 0f, 1f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Down"), "Axis 8", -1f, 0f, isDpad: true));
        m_dualShockTriggers.Add(new DualShockTrigger(m_buttonContainer.Find("LeftTrigger"), "Axis 4"));
        m_dualShockTriggers.Add(new DualShockTrigger(m_buttonContainer.Find("RightTrigger"), "Axis 5"));

#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        button_map.Add("Button0", "Square");
        button_map.Add("Button1", "Cross");
        button_map.Add("Button2", "Circle");
        button_map.Add("Button3", "Triangle");
        button_map.Add("Button4", "LeftShoulder");
        button_map.Add("Button5", "RightShoulder");
        //button_map.Add("Button6", "LeftTrigger");
        //button_map.Add("Button7", "RightTrigger");
        button_map.Add("Button8", "Select");
        button_map.Add("Button9", "Start");
        button_map.Add("Button10", "LeftStick/Stick - Input Manager");
        button_map.Add("Button11", "RightStick/Stick - Input Manager");
        button_map.Add("Button12", "SystemButton");
        button_map.Add("Button13", "TouchpadButton");
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("LeftStick/Stick - Input Manager"), "Axis 1", "Axis 2", posText: m_leftStickText, isYReversed: true));
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("RightStick/Stick - Input Manager"), "Axis 3", "Axis 4", posText: m_rightStickText, isYReversed: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Left"), "Axis 7", -1f, 0f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Right"), "Axis 7", 0f, 1f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Down"), "Axis 8", 0f, 1f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Up"), "Axis 8", -1f, 0f, isDpad: true));
        m_dualShockTriggers.Add(new DualShockTrigger(m_buttonContainer.Find("LeftTrigger"), "Axis 5"));
        m_dualShockTriggers.Add(new DualShockTrigger(m_buttonContainer.Find("RightTrigger"), "Axis 6"));

#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_LINUX_API
        button_map.Add("Button0", "Cross");
        button_map.Add("Button1", "Circle");
        button_map.Add("Button2", "Square");
        button_map.Add("Button3", "Triangle");
        button_map.Add("Button4", "LeftShoulder");
        button_map.Add("Button5", "RightShoulder");
        button_map.Add("Button6", "Select");
        button_map.Add("Button7", "Start");
        button_map.Add("Button8", "SystemButton");
        button_map.Add("Button9", "LeftStick/Stick - Input Manager");
        button_map.Add("Button10", "RightStick/Stick - Input Manager");
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("LeftStick/Stick - Input Manager"), "Axis 1", "Axis 2", posText: m_leftStickText, isYReversed: true));
        analog_sticks.Add(new AnalogStick(m_buttonContainer.Find("RightStick/Stick - Input Manager"), "Axis 4", "Axis 5", posText: m_rightStickText, isYReversed: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Left"), "Axis 7", -1f, 0f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Right"), "Axis 7", 0f, 1f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Up"), "Axis 8", -1f, 0f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("Dpad/Down"), "Axis 8", 0f, 1f, isDpad: true));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("LeftTrigger"), "Axis 3", 0f, 1f));
        analog_buttons.Add(new AnalogButton(m_buttonContainer.Find("RightTrigger"), "Axis 6", 0f, 1f));
#endif
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAllButtons();
        UpdateAllAnalogSticks();
        UpdateAllAnalogButtons();

 #if !(UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_LINUX_API)
        foreach (DualShockTrigger trigger in m_dualShockTriggers)
        {
            float inputValue = Input.GetAxis(trigger.Axis_Name);
            if (trigger.IsPressed(inputValue))
                StartHighlightButton(trigger.Name);
            else
                StopHighlightButton(trigger.Name);
        }
 #endif
    }

    // When a stick is pressed, change the color instead of using Particles.
    protected override void StartHighlightButton(string buttonName)
    {
        Transform button = m_buttonContainer.Find(buttonName);
        if (button == null)
            ShowMessage(buttonName);
        else if (buttonName.Contains("Stick"))
            button.GetComponent<SpriteRenderer>().color = Color.blue;
        else
        {
            ParticleSystem ps = button.GetComponentInChildren<ParticleSystem>();
            if (ps == null)
                Instantiate(m_buttonHighlight, button.position - new Vector3(0f, 0f, 0.1f), button.rotation, button);
            else
                ps.Play();
        }
    }

    protected override void StopHighlightButton(string buttonName)
    {
        Transform button = m_buttonContainer.Find(buttonName);
        if (buttonName.Contains("Stick"))
            button.GetComponent<SpriteRenderer>().color = m_stickButtonColor;
        else if (button != null)
        {
            ParticleSystem[] ps = button.GetComponentsInChildren<ParticleSystem>();
            if (ps.Length > 0)
            {
                foreach (ParticleSystem p in ps)
                    p.Stop();
            }
        }
    }

#endif
}

// This is for DualShock controller triggers on Windows and OSX
// The trigger starts at 0 until it is first used. Then the range is [-1, 1].
public class DualShockTrigger : AnalogButton
{
    private bool is_first = true;

    public DualShockTrigger(Transform trigger, string axisName) : base(trigger, axisName) {}

    public override bool IsPressed(float inputValue)
    {
        if (is_first)
        {
            if (inputValue == 0.00f)
                return false;
            else
            {
                is_first = false;
                return IsPressed(inputValue);
            }
        }
        else
        {
            return inputValue > (deadzone - 1);
        }
    }
}
