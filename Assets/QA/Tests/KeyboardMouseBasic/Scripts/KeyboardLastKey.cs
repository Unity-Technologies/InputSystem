using System;
using UnityEngine;

using UnityEngine.Experimental.Input;

using UnityEngine.UI;

// Print the last key based from the Keyboard.onTextInput event
//
public class KeyboardLastKey : MonoBehaviour
{
    [Header("If left empty, will try to auto populate with GetComponent<Text>()")]
    public Text reportText;

    private Keyboard m_Keyboard;
    private char m_LastKeyPressed;

    void OnEnable()
    {
        if (reportText == null && GetComponent<Text>() != null)
        {
            reportText = GetComponent<Text>();
        }

        SetCurrentKB();
    }

    private void Update()
    {
        if (m_Keyboard != null && InputSystem.GetDevice<Keyboard>() != null && m_Keyboard != InputSystem.GetDevice<Keyboard>())
        {
            ReleaseKB();
            SetCurrentKB();
        }
        else if (m_Keyboard == null)
        {
            SetCurrentKB();
        }
    }

    private void OnDisable()
    {
        ReleaseKB();
    }

    void RecordKey(char c)
    {
        reportText.text = "0x" + ((int)c).ToString("X4") + " => ";

        if (char.IsControl(c) || ((int)c <= 32))
        {
            reportText.text += StringForNonPrintable(c);
        }
        else
        {
            reportText.text += c.ToString();
        }
    }

    void SetCurrentKB()
    {
        if (InputSystem.GetDevice<Keyboard>() == null) { return; }

        m_Keyboard = InputSystem.GetDevice<Keyboard>();
        m_Keyboard.onTextInput += new Action<char>(RecordKey);
    }

    void ReleaseKB()
    {
        if (m_Keyboard != null)
        {
            m_Keyboard.onTextInput -= new Action<char>(RecordKey);
        }
    }

    // Assumes ascii input
    // TODO - expand to wider unicode if necessary
    //
    String StringForNonPrintable(char ascii)
    {
        switch ((int)ascii)
        {
            case 0:
                return "Null";
            case 1:
                return "Start of Heading";
            case 2:
                return "Start of Text";
            case 3:
                return "End of Text";
            case 4:
                return "End of Transmission";
            case 5:
                return "Enquiry";
            case 6:
                return "Acknowledge";
            case 7:
                return "Bell";
            case 8:
                return "Backspace";
            case 9:
                return "Horizontal Tab";
            case 10:
                return "Line Feed";
            case 11:
                return "Vertical Tab";
            case 12:
                return "Form Feed";
            case 13:
                return "Carriage Return";
            case 14:
                return "Shift Out";
            case 15:
                return "Shift In";
            case 16:
                return "Data Link Escape";
            case 17:
                return "Device Control 1";
            case 18:
                return "Device Control 2";
            case 19:
                return "Device Control 3";
            case 20:
                return "Device Control 4";
            case 21:
                return "Negative Acknowledge";
            case 22:
                return "Synchronous Idle";
            case 23:
                return "Eng of Trans. Block";
            case 24:
                return "Cancel";
            case 25:
                return "End of Medium";
            case 26:
                return "Substitute";
            case 27:
                return "Escape";
            case 28:
                return "File Separator";
            case 29:
                return "Group Separator";
            case 30:
                return "Record Separator";
            case 31:
                return "Unit Separator";
            case 32:
                return "Space";
            case 127:
                return "Delete";
            default:
                return "Printable Descriptor not found";
        }
    }
}
