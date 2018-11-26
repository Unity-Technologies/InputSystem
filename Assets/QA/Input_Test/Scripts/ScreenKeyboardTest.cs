using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;


public class ScreenKeyboardTest : MonoBehaviour
{
    public Dropdown m_KeyboardTypeDropDown;
    public Toggle m_KeyboardAutocorrection;
    public Toggle m_KeyboardMultiline;
    public Toggle m_KeyboardSecure;
    public Toggle m_KeyboardAlert;
    public InputField m_InputField;

    ScreenKeyboard m_ScreenKeyboard;
    // Start is called before the first frame update
    void Start()
    {
        m_ScreenKeyboard = InputSystem.AddDevice<AndroidScreenKeyboard>();
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
    }

    public void Show()
    {
        ScreenKeyboardShowParams showParams = new ScreenKeyboardShowParams()
        {
            initialText = m_InputField.text,
            autocorrection = m_KeyboardAutocorrection.isOn,
            multiline = m_KeyboardMultiline.isOn,
            secure = m_KeyboardSecure.isOn,
            alert = m_KeyboardAlert.isOn
        };

        m_ScreenKeyboard.Show(showParams);
    }

    public void Hide()
    {
        m_ScreenKeyboard.Hide();
    }
}
