using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input;

public class IMETest : MonoBehaviour
{
    public UnityEngine.Experimental.Input.IMECompositionMode mode;
    public Vector2 cursorPosition;

    public string outputString;
    public string compositionString;

    private bool m_AddedTextListener = false;
    private bool m_AddedIMEListener = false;

    // Use this for initialization
    void OnEnable ()
    {
        if(!m_AddedTextListener)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyboard.onTextInput += OnTextEvent;
                m_AddedTextListener = true;
            }
        }

        if(!m_AddedIMEListener)
        {
            IMEDevice ime = IMEDevice.current;
            if(ime != null)
            {
                ime.onIMECompositionChange += OnIMECompositionChange;
                m_AddedIMEListener = true;
            }
        }
	}

    void OnDisable()
    {
        if(m_AddedTextListener)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyboard.onTextInput -= OnTextEvent;
            }
            m_AddedTextListener = false;
        }

        if (m_AddedIMEListener)
        {
            IMEDevice ime = IMEDevice.current;
            if (ime != null)
            {
                ime.onIMECompositionChange += OnIMECompositionChange;
                m_AddedIMEListener = false;
            }
        }
    }

    void OnTextEvent(char character)
    {
        outputString += character;
    }

    void OnIMECompositionChange(string compositionString)
    {
        this.compositionString = compositionString;
    }

    // Update is called once per frame
    void Update ()
    {
        if (!m_AddedTextListener)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyboard.onTextInput += OnTextEvent;
                m_AddedTextListener = true;
            }
        }

        if (!m_AddedIMEListener)
        {
            IMEDevice ime = IMEDevice.current;
            if (ime != null)
            {
                ime.onIMECompositionChange += OnIMECompositionChange;
                m_AddedIMEListener = true;
            }
        }

        IMEDevice device = IMEDevice.current;
        device.mode = mode;
        device.position = cursorPosition;
	}
}
