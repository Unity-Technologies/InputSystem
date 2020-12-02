using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public enum AutomaticOperation
{
    None,
    CharacterLimit,
    LetterReplacement,
    DismissOnCharacter0
}

public class ScreenKeyboardTestScript : MonoBehaviour
{
    private const int kTextLimit = 5;

    public Dropdown m_Properties;
    public GameObject m_ShowParams;
    public GameObject m_SpecialBehavior;

    public Dropdown m_KeyboardTypeDropDown;
    public Toggle m_KeyboardAutocorrection;
    public Toggle m_KeyboardMultiline;
    public Toggle m_KeyboardSecure;
    public Toggle m_KeyboardAlert;
    public Toggle m_KeyboardInputFieldHidden;
    public Toggle m_KeyboardLogging;
    public InputField m_InputDeviceInfo;
    public InputField m_InputField;
    public InputField m_OccludingAreaField;
    public InputField m_KeyboardStatus;
    public InputField m_KeyboardInputField;

    public InputField m_OldOccludingAreaField;
    public InputField m_OldKeyboardStatus;
    public InputField m_OldKeyboardInputField;

    public Dropdown m_AutomaticOperation;
    public Text m_SpecialBehaviorInfo;

    public GameObject m_Info;
    public GameObject m_Log;
    public Scrollbar m_VerticalScrollbar;

    public Text m_LogText;

    ScreenKeyboard m_ScreenKeyboard;

    TouchScreenKeyboard m_OldScreenKeyboard;

    private ScreenKeyboardCallbacks m_Callbacks;

    void Start()
    {
        m_ScreenKeyboard = InputSystem.screenKeyboard;
        m_KeyboardTypeDropDown.ClearOptions();
        m_AutomaticOperation.ClearOptions();

        m_Callbacks = new ScreenKeyboardCallbacks();
        m_Callbacks.stateChanged = StateChangedCallback;
        m_Callbacks.inputFieldTextChanged = InputFieldTextCallback;
        m_Callbacks.inputFieldSelectionChanged = SelectionChanged;


        m_Properties.ClearOptions();
        m_Properties.options.Add(new Dropdown.OptionData("Show Params"));
        m_Properties.options.Add(new Dropdown.OptionData("Special Behavior"));
        m_Properties.RefreshShownValue();
        m_Properties.onValueChanged.AddListener(PropertiesSelectionChanged);
        PropertiesSelectionChanged(m_Properties.value);
        SetLogging();

        foreach (var t in Enum.GetValues(typeof(ScreenKeyboardType)))
        {
            m_KeyboardTypeDropDown.options.Add(new Dropdown.OptionData(t.ToString()));
        }
        m_KeyboardTypeDropDown.RefreshShownValue();

        foreach (var t in Enum.GetValues(typeof(AutomaticOperation)))
        {
            m_AutomaticOperation.options.Add(new Dropdown.OptionData(t.ToString()));
        }
        m_AutomaticOperation.RefreshShownValue();

        m_LogText.text = "";
    }

    private void PropertiesSelectionChanged(int value)
    {
        if (value == 0)
        {
            m_ShowParams.SetActive(true);
            m_SpecialBehavior.SetActive(false);
        }
        else
        {
            m_ShowParams.SetActive(false);
            m_SpecialBehavior.SetActive(true);
        }
    }

    private int CountOccurences(string text, char letter)
    {
        int count = 0;
        foreach (char c in text)
            if (c == letter) count++;
        return count;
    }

    private void Log(string format, params object[] list)
    {
        m_LogText.text += $"Frame[{Time.frameCount}] " + string.Format(format, list) + "\n";
        var lineCount = CountOccurences(m_LogText.text, '\n');
        float lineHeight = 16.0f;
        // Has to be a better way
        var value = (lineCount - (366 / lineHeight) + 4) * (lineHeight / 1980.0f);
        if (value < 0.0)
            value = 0.0f;

        //m_VerticalScrollbar.value = 1.0f - value;
    }

    private void SelectionChanged(RangeInt obj)
    {
        Log($"Selection: {obj.start}, {obj.length}");
    }

    private void InputFieldTextCallback(string text)
    {
        Log($"Text: {text}");
        var oldText = text;
        AutomaticOperation op = (AutomaticOperation)Enum.Parse(typeof(AutomaticOperation), m_AutomaticOperation.captionText.text);
        switch (op)
        {
            case AutomaticOperation.CharacterLimit:
                if (text.Length > kTextLimit)
                    text = text.Substring(0, kTextLimit);
                break;
            case AutomaticOperation.LetterReplacement:
                text = text.Replace("a", "c");
                break;
            case AutomaticOperation.DismissOnCharacter0:
                if (text.Contains("0"))
                {
                    Log($"Disable on 0");
                    m_ScreenKeyboard.Hide();
                }
                break;
        }

        m_ScreenKeyboard.inputFieldText = text;
        m_InputField.text = text;
    }

    private void StateChangedCallback(ScreenKeyboardState state)
    {
        Log($"Status: {state}");
    }

    void Update()
    {
        var area = m_ScreenKeyboard.occludingArea;
        m_OccludingAreaField.text = $"{area.xMin}, {area.yMin}, {area.width}, {area.height}";
        m_KeyboardStatus.text = m_ScreenKeyboard.state.ToString();
        m_KeyboardInputField.text = m_ScreenKeyboard.inputFieldText;

        if (m_OldScreenKeyboard != null)
        {
            m_OldOccludingAreaField.text = $"{Screen.width} {Screen.height}";//  TouchScreenKeyboard.area.ToString();
            m_OldKeyboardStatus.text = m_OldScreenKeyboard.status.ToString();
            m_OldKeyboardInputField.text = m_OldScreenKeyboard.text;
        }


        var newVisible = m_ScreenKeyboard.state == ScreenKeyboardState.Visible;
        var oldVisible = TouchScreenKeyboard.visible;
        #if UNITY_IOS
        // On iOS TouchScreenKeyboard.visible checks for keyboard availability globally
        // That means if we show our input system screen keyboard, it will return true
        // This doesn't happen on Android
        // Workaround this issue for now...
        if (newVisible)
            oldVisible = false;
        #endif

        var infoMessage = $"FrameCount: {Time.frameCount} ";
        if (oldVisible && newVisible)
        {
            infoMessage += "ERROR: both new and old screen keyboards are visible ?";
        }
        else if (oldVisible)
        {
            infoMessage += "[OLD] ScreenKeyboard";
            if (m_OldScreenKeyboard != null)
                infoMessage += $@" Status: {m_OldScreenKeyboard.status}
Selection: {m_OldScreenKeyboard.selection.start}, {m_OldScreenKeyboard.selection.length}, {m_OldScreenKeyboard.text}
";
        }
        else if (newVisible)
        {
            infoMessage += $@"[NEW] ScreenKeyboard Status: {m_ScreenKeyboard.state}
Selection: {m_ScreenKeyboard.selection.start}, {m_ScreenKeyboard.selection.length}, {m_ScreenKeyboard.inputFieldText}
";
        }
        else
        {
            infoMessage += "No keyboard is shown";
        }

        m_InputDeviceInfo.text = infoMessage;

        AutomaticOperation op = (AutomaticOperation)Enum.Parse(typeof(AutomaticOperation), m_AutomaticOperation.captionText.text);
        switch (op)
        {
            case AutomaticOperation.CharacterLimit:
                m_SpecialBehaviorInfo.text = "Character length will be limited to " + kTextLimit;
                break;
            case AutomaticOperation.LetterReplacement:
                m_SpecialBehaviorInfo.text = "a letter will be replaced by c";
                break;
            case AutomaticOperation.DismissOnCharacter0:
                m_SpecialBehaviorInfo.text = "Keyboard will hide on character 0";
                break;
        }
    }

    private ScreenKeyboardType ToScreenKeyboardType(string value)
    {
        return (ScreenKeyboardType)Enum.Parse(typeof(ScreenKeyboardType), value);
    }

    public void Show()
    {
        ScreenKeyboardShowParams showParams = new ScreenKeyboardShowParams()
        {
            initialText = m_InputField.text,
            autocorrection = m_KeyboardAutocorrection.isOn,
            multiline = m_KeyboardMultiline.isOn,
            secure = m_KeyboardSecure.isOn,
            alert = m_KeyboardAlert.isOn,
            inputFieldHidden = m_KeyboardInputFieldHidden.isOn,
            type = ToScreenKeyboardType(m_KeyboardTypeDropDown.captionText.text)
        };

        m_ScreenKeyboard.Show(showParams, m_Callbacks);

        Log($"Requesting keyboard to show");
    }

    private TouchScreenKeyboardType ToTouchScreenKeyboardType(string value)
    {
        return (TouchScreenKeyboardType)Enum.Parse(typeof(TouchScreenKeyboardType), value);
    }

    public void ShowOldKeyboard()
    {
        TouchScreenKeyboard.hideInput = m_KeyboardInputFieldHidden.isOn;
        m_OldScreenKeyboard = TouchScreenKeyboard.Open(m_InputField.text,
            ToTouchScreenKeyboardType(m_KeyboardTypeDropDown.captionText.text),
            m_KeyboardAutocorrection.isOn,
            m_KeyboardMultiline.isOn,
            m_KeyboardSecure.isOn,
            m_KeyboardAlert.isOn,
            "No placeholder");
    }

    public void ShowInfo()
    {
        m_Info.SetActive(true);
        m_Log.SetActive(false);
    }

    public void ShowLog()
    {
        m_Info.SetActive(false);
        m_Log.SetActive(true);
    }

    public void ClearLog()
    {
        m_LogText.text = "";
    }

    public void Hide()
    {
        m_ScreenKeyboard.Hide();
    }

    public void HideMobileInputField()
    {
        m_InputField.shouldHideMobileInput = m_KeyboardInputFieldHidden.isOn;
    }

    public void SetLogging()
    {
        m_ScreenKeyboard.logging = m_KeyboardLogging.isOn;
    }

    public Rect GetOccludingArea()
    {
        if (m_ScreenKeyboard.state == ScreenKeyboardState.Visible)
            return m_ScreenKeyboard.occludingArea;
        else if (TouchScreenKeyboard.visible)
            return TouchScreenKeyboard.area;
        return Rect.zero;
    }
}
