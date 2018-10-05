using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.UI;

public class IMETest : MonoBehaviour
{
    public bool enableIME;
    public bool activeIME;
    public Vector2 cursorPosition;

    public string outputString;
    public string compositionString;

    private bool m_AddedTextListeners = false;

    //UI Visual Support
    public Toggle activeIMEVisual;
    public Toggle enabledIMEVisual;
    public InputField outputStringText;
    public InputField compositionStringText;
    public InputField cursorXInput;
    public InputField cursorYInput;
    public Button cursorPositionButton;

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

        OnCursorTextEntered();

        if (enabledIMEVisual != null)
            enabledIMEVisual.isOn = enableIME;
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

        if (outputStringText != null)
            outputStringText.text = outputString;
    }

    void OnIMECompositionChange(IMEComposition composition)
    {
        compositionString = "";
        foreach (char c in composition)
            compositionString += c;

        if (compositionStringText != null)
            compositionStringText.text = compositionString;
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

            activeIME = keyboard.imeSelected.isPressed;

            if (activeIMEVisual != null)
                activeIMEVisual.isOn = activeIME;
        }
    }

    public void OnCursorTextEntered()
    {
        if (cursorXInput != null && cursorYInput != null && cursorPositionButton != null)
        {
            float x, y;
            bool validInput = float.TryParse(cursorXInput.text, out x);
            validInput &= float.TryParse(cursorYInput.text, out y);

            cursorPositionButton.interactable = validInput;
        }
    }

    public void OnClearOutputString()
    {
        outputString = "";

        if (outputStringText != null)
            outputStringText.text = outputString;
    }

    public void OnEnabledToggleChanged(bool newState)
    {
        enableIME = newState;
    }

    public void OnSubmitCursorPosition()
    {
        Keyboard keyboard = Keyboard.current;
        if (cursorXInput != null && cursorYInput != null && keyboard != null)
        {
            float x, y;
            bool validInput = float.TryParse(cursorXInput.text, out x);
            validInput &= float.TryParse(cursorYInput.text, out y);

            if (validInput)
            {
                keyboard.imeCursorPosition = cursorPosition = new Vector2(x, y);
            }
        }
    }
}
