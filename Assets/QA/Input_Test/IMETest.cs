using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input;

public class IMETest : MonoBehaviour
{
    public bool enableIME;
    public Vector2 cursorPosition;

    public string outputString;
    public string compositionString;

    private bool m_AddedTextListeners = false;

    // Use this for initialization
    void OnEnable()
    {
        if (!m_AddedTextListeners)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyboard.onTextInput += OnTextEvent;
                keyboard.onIMECompositionChange += OnIMECompositionChange;
                m_AddedTextListeners = true;
            }
        }
    }

    void OnDisable()
    {
        if (m_AddedTextListeners)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyboard.onTextInput -= OnTextEvent;
                keyboard.onIMECompositionChange -= OnIMECompositionChange;
            }
            m_AddedTextListeners = false;
        }
    }

    void OnTextEvent(char character)
    {
        outputString += character;
    }

    void OnIMECompositionChange(IMEComposition composition)
    {
        compositionString = "";
        for (int i = 0; i < composition.Count; i++)
            compositionString += composition[i];
    }

    // Update is called once per frame
    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (!m_AddedTextListeners)
            {
                keyboard.onTextInput += OnTextEvent;
                keyboard.onIMECompositionChange += OnIMECompositionChange;
                m_AddedTextListeners = true;
            }

            keyboard.imeEnabled = enableIME;
            keyboard.imeCursorPosition = cursorPosition;
        }
    }
}
