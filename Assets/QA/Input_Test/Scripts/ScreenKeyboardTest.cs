using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;

#if UNITY_ANDROID
using UnityEngine.Experimental.Input.Plugins.Android;
#elif UNITY_WSA
using UnityEngine.Experimental.Input.Plugins.WSA;
#endif

public class ScreenKeyboardTest : MonoBehaviour
{
    public Dropdown m_KeyboardTypeDropDown;
    public Toggle m_KeyboardAutocorrection;
    public Toggle m_KeyboardMultiline;
    public Toggle m_KeyboardSecure;
    public Toggle m_KeyboardAlert;
    public InputField m_InputField;
    public InputField m_OccludingAreaField;
    public Toggle m_Visible;
    public InputField m_KeyboardInputField;

    ScreenKeyboard m_ScreenKeyboard;
    // Start is called before the first frame update
    void Start()
    {
        m_ScreenKeyboard = ScreenKeyboard.GetInstance();
        m_KeyboardTypeDropDown.ClearOptions();
    

        foreach (var t in Enum.GetValues(typeof(ScreenKeyboardType)))
        {
            m_KeyboardTypeDropDown.options.Add(new Dropdown.OptionData(t.ToString()));
        }
        m_KeyboardTypeDropDown.RefreshShownValue();
    }

    // Update is called once per frame
    void Update()
    {
        m_OccludingAreaField.text = m_ScreenKeyboard.occludingArea.ToString();
        m_Visible.isOn = m_ScreenKeyboard.visible;
        m_KeyboardInputField.text = m_ScreenKeyboard.inputFieldText;
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
        TouchScreenKeyboard.Open(m_InputField.text,
            ToTouchScreenKeyboardType(m_KeyboardTypeDropDown.captionText.text),
            m_KeyboardAutocorrection.isOn,
            m_KeyboardMultiline.isOn,
            m_KeyboardSecure.isOn,
            m_KeyboardAlert.isOn,
            "No placeholder");
    }

    public void Hide()
    {
        m_ScreenKeyboard.Hide();
    }
}
