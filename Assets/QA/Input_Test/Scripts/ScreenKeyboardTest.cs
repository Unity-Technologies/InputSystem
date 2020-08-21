using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

#if UNITY_ANDROID
using UnityEngine.InputSystem.Android;
#elif UNITY_WSA
using UnityEngine.InputSystem.WSA;
#endif

public enum AutomaticOperation
{
    None,
    CharacterLimit,
    LetterReplacement
}

public class ScreenKeyboardTest : MonoBehaviour
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
    // Start is called before the first frame update

    TouchScreenKeyboard m_OldScreenKeyboard;

    void Start()
    {
        var canvasScaler = GetComponent<CanvasScaler>();
#if UNITY_WSA
        canvasScaler.enabled = false;
#endif
        Log("sds");
        m_ScreenKeyboard = InputSystem.GetDevice<ScreenKeyboard>();
        m_KeyboardTypeDropDown.ClearOptions();
        m_AutomaticOperation.ClearOptions();


        m_ScreenKeyboard.stateChanged += StateChangedCallback;
        m_ScreenKeyboard.inputFieldTextChanged += InputFieldTextCallback;
        m_ScreenKeyboard.selectionChanged += SelectionChanged;

        m_Properties.ClearOptions();
        m_Properties.options.Add(new Dropdown.OptionData("Show Params"));
        m_Properties.options.Add(new Dropdown.OptionData("Special Behavior"));
        m_Properties.RefreshShownValue();
        m_Properties.onValueChanged.AddListener(PropertiesSelectionChanged);

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
        m_LogText.text += string.Format(format, list) + "\n";
        var lineCount = CountOccurences(m_LogText.text, '\n');
        float lineHeight = 16.0f;
        // Has to be a better way
        var value = (lineCount - (366 / lineHeight) + 4) * (lineHeight / 1980.0f);
        if (value < 0.0)
            value = 0.0f;

        m_VerticalScrollbar.value = 1.0f - value;
    }

    private void SelectionChanged(RangeInt obj)
    {
        Log($"Selection: {obj.start}, {obj.length}");
    }

    private void InputFieldTextCallback(string text)
    {
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
        }

        if (text != oldText)
        {
            m_ScreenKeyboard.inputFieldText = text;
        }

        Log($"Text: {text}");
        m_InputField.text = text;
    }

    private void StateChangedCallback(ScreenKeyboardStatus status)
    {
        Log($"Status: {status}");
    }

    // Update is called once per frame
    void Update()
    {
        m_OccludingAreaField.text = m_ScreenKeyboard.occludingArea.ToString();
        m_KeyboardStatus.text = m_ScreenKeyboard.status.ToString();
        m_KeyboardInputField.text = m_ScreenKeyboard.inputFieldText;

        if (m_OldScreenKeyboard != null)
        {
            m_OldOccludingAreaField.text = TouchScreenKeyboard.area.ToString();
            m_OldKeyboardStatus.text = m_OldScreenKeyboard.status.ToString();
            m_OldKeyboardInputField.text = m_OldScreenKeyboard.text;
        }

        var oldVisible = TouchScreenKeyboard.visible;
        var newVisible = m_ScreenKeyboard.enabled;

        if (oldVisible && newVisible)
        {
            m_InputDeviceInfo.text = "ERROR: both new and old screen keyboards are visible ?";
        }
        else if (oldVisible)
        {
            m_InputDeviceInfo.text = "Showing old TouchscreenKeyboard";
        }
        else if (newVisible)
        {
            m_InputDeviceInfo.text = $@"Name: {m_ScreenKeyboard.name} Enabled: {m_ScreenKeyboard.enabled}
Selection: {m_ScreenKeyboard.selection.start}, {m_ScreenKeyboard.selection.length}
";
        }
        else
        {
            m_InputDeviceInfo.text = "No keyboard is shown";
        }

        AutomaticOperation op = (AutomaticOperation)Enum.Parse(typeof(AutomaticOperation), m_AutomaticOperation.captionText.text);
        switch (op)
        {
            case AutomaticOperation.CharacterLimit:
                m_SpecialBehaviorInfo.text = "Character length will be limited to " + kTextLimit;
                break;
            case AutomaticOperation.LetterReplacement:
                m_SpecialBehaviorInfo.text = "a letter will be replaced by c";
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

        m_ScreenKeyboard.Show(showParams);
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
}
