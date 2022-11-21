using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

////FIXME: will not work properly when there are multiple keyboards

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

    public void OnEnable()
    {
        if (!m_AddedTextListeners)
        {
            var keyboard = InputSystem.GetDevice<Keyboard>();
            if (keyboard != null)
            {
                keyboard.onTextInput += OnTextEvent;
                keyboard.onIMECompositionChange += OnIMECompositionChange;
                keyboard.onBufferedIMECompositionChange += OnIMECompositionChange;
                m_AddedTextListeners = true;
            }
        }

        OnCursorTextEntered();

        if (enabledIMEVisual != null)
            enabledIMEVisual.isOn = enableIME;
    }

    public void OnDisable()
    {
        if (!m_AddedTextListeners)
            return;

        var keyboard = InputSystem.GetDevice<Keyboard>();
        if (keyboard != null)
        {
            keyboard.onTextInput -= OnTextEvent;
            keyboard.onIMECompositionChange -= OnIMECompositionChange;
            keyboard.onBufferedIMECompositionChange -= OnIMECompositionChange;
        }
        m_AddedTextListeners = false;
    }

    private void OnTextEvent(char character)
    {
        outputString += character;

        if (outputStringText != null)
            outputStringText.text = outputString;
    }

    private void OnIMECompositionChange(IMECompositionString compositionString)
    {
        this.compositionString = "";
        foreach (var c in compositionString)
            this.compositionString += c;

        if (compositionStringText != null)
            compositionStringText.text = this.compositionString;
    }

    private void OnIMECompositionChange(char[] buffer, int length)
    {
	    compositionString = "";
	    for (var i = 0; i < length; i++)
		    compositionString += buffer[i];

	    if (compositionStringText != null)
		    compositionStringText.text = compositionString;
    }

    public void Update()
    {
        var keyboard = InputSystem.GetDevice<Keyboard>();
        if (keyboard == null)
            return;

        if (!m_AddedTextListeners)
        {
            keyboard.onTextInput += OnTextEvent;
            keyboard.onIMECompositionChange += OnIMECompositionChange;
            m_AddedTextListeners = true;
        }

        keyboard.SetIMEEnabled(enableIME);
        keyboard.SetIMECursorPosition(cursorPosition);

        activeIME = keyboard.imeSelected.isPressed;

        if (activeIMEVisual != null)
            activeIMEVisual.isOn = activeIME;
    }

    public void OnCursorTextEntered()
    {
        if (cursorXInput == null || cursorYInput == null || cursorPositionButton == null)
            return;

        float x, y;
        var validInput = float.TryParse(cursorXInput.text, out x);
        validInput &= float.TryParse(cursorYInput.text, out y);

        cursorPositionButton.interactable = validInput;
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
        var keyboard = InputSystem.GetDevice<Keyboard>();
        if (cursorXInput == null || cursorYInput == null || keyboard == null)
            return;

        float x, y;
        var validInput = float.TryParse(cursorXInput.text, out x);
        validInput &= float.TryParse(cursorYInput.text, out y);

        if (validInput)
        {
            cursorPosition = new Vector2(x, y);
            keyboard.SetIMECursorPosition(cursorPosition);
        }
    }
}
