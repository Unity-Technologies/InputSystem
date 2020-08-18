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
    public Dropdown m_KeyboardTypeDropDown;
    public Toggle m_KeyboardAutocorrection;
    public Toggle m_KeyboardMultiline;
    public Toggle m_KeyboardSecure;
    public Toggle m_KeyboardAlert;
    public Toggle m_KeyboardInputFieldHidden;
    public InputField m_InputField;
    public InputField m_OccludingAreaField;
    public InputField m_KeyboardStatus;
    public InputField m_KeyboardInputField;

    public InputField m_OldOccludingAreaField;
    public InputField m_OldKeyboardStatus;
    public InputField m_OldKeyboardInputField;

    public Dropdown m_AutomaticOperation;

    public GameObject m_Info;
    public GameObject m_Log;

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
        m_ScreenKeyboard = InputSystem.GetDevice<ScreenKeyboard>();
        m_KeyboardTypeDropDown.ClearOptions();
        m_AutomaticOperation.ClearOptions();

        m_ScreenKeyboard.statusChanged += StatusChangedCallback;
        m_ScreenKeyboard.onIMECompositionChange += IMECompositionChange;


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

    private void IMECompositionChange(UnityEngine.InputSystem.LowLevel.IMECompositionString obj)
    {
        var text = obj.ToString();
        var oldText = text;
        AutomaticOperation op = (AutomaticOperation)Enum.Parse(typeof(AutomaticOperation), m_AutomaticOperation.captionText.text);
        switch (op)
        {
            case AutomaticOperation.CharacterLimit:
                if (text.Length > 5)
                    text = text.Substring(0, 5);
                break;
            case AutomaticOperation.LetterReplacement:
                text = text.Replace("a", "c");
                break;
        }

        if (text != oldText)
        {
            m_ScreenKeyboard.inputFieldText = text;
        }
        m_LogText.text += "IME:" + text + Environment.NewLine;
        m_InputField.text = text;
    }

    private void StatusChangedCallback(ScreenKeyboardState state)
    {
        m_LogText.text += "Status: " + state + Environment.NewLine;
    }

    // Update is called once per frame
    void Update()
    {
        m_OccludingAreaField.text = m_ScreenKeyboard.occludingArea.ToString();
        m_KeyboardStatus.text = m_ScreenKeyboard.state.ToString();
        m_KeyboardInputField.text = m_ScreenKeyboard.inputFieldText;

        if (m_OldScreenKeyboard != null)
        {
            m_OldOccludingAreaField.text = TouchScreenKeyboard.area.ToString();
            m_OldKeyboardStatus.text = m_OldScreenKeyboard.status.ToString();
            m_OldKeyboardInputField.text = m_OldScreenKeyboard.text;
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
