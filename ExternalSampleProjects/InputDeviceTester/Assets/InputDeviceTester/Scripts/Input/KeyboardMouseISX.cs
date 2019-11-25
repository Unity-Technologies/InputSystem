using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class KeyboardMouseISX : MonoBehaviour
{
    [Tooltip("Highlight Prefab")]
    public SpriteRenderer m_keyHighlight;

    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    [Header("UI Elements for Debug Info")]
    public Text m_keyboardInfoText;
    public Text m_mouseInfoText;

    private InputAction m_keyboardAction;
    private InputAction m_mouseButtonAction;
    private InputAction m_mouseOtherAction;

    private Keyboard m_registeredKeyboard;

    private const int MOUSE_MOVE_DEADZONE = 5;

    // Use this for initialization
    void Start()
    {
        m_keyboardAction = new InputAction(name: "KeyboardPressAction", InputActionType.PassThrough,
            binding: "<keyboard>/<key>");
        m_keyboardAction.performed += callbackContext => KeyboardKeyPress(callbackContext.control as KeyControl);
        m_keyboardAction.Enable();

        m_mouseButtonAction = new InputAction(name: "MouseButtonAction", InputActionType.PassThrough,
            binding: "<mouse>/<button>");
        m_mouseButtonAction.performed += callbackContext => MouseKeyPress(callbackContext.control as ButtonControl);
        m_mouseButtonAction.Enable();

        m_mouseOtherAction = new InputAction(name: "MouseVector2Action", InputActionType.PassThrough,
            binding: "<mouse>/<vector2>");
        m_mouseOtherAction.performed += callbackContext => MouseVector2(callbackContext.control as Vector2Control);
        m_mouseOtherAction.Enable();
    }

    private void OnEnable()
    {
        m_keyboardAction?.Enable();
        m_mouseButtonAction?.Enable();
        m_mouseOtherAction?.Enable();

        StartCoroutine(nameof(EnableTrackKeyboardInput));
    }

    private void OnDisable()
    {
        m_keyboardAction.Disable();
        m_mouseButtonAction.Disable();
        m_mouseOtherAction.Disable();

        if (m_registeredKeyboard != null)
            m_registeredKeyboard.onTextInput -= new Action<char>(RecordKey);
    }

    void Update()
    {
        // Show mouse actions
        Mouse mouse = InputSystem.GetDevice<Mouse>();
        if (mouse != null)
        {
            // Update mouse position
            m_mouseInfoText.text = mouse.position.ReadValue().ToString("F0") + "\n"
                + mouse.scroll.ReadValue().ToString() + "\n"
                + mouse.delta.ReadValue().ToString("F3");
        }

        Keyboard keyboard = InputSystem.GetDevice<Keyboard>();
    }

    // There is a delay in getting current keyboard. For OnEnable to assign event
    private IEnumerator EnableTrackKeyboardInput()
    {
        yield return new WaitUntil(() => InputSystem.GetDevice<Keyboard>() != null);

        m_registeredKeyboard = InputSystem.GetDevice<Keyboard>();
        m_registeredKeyboard.onTextInput -= new Action<char>(RecordKey);
        m_registeredKeyboard.onTextInput += new Action<char>(RecordKey);
    }

    private void RecordKey(char c)
    {
        if (char.IsControl(c) || ((int)c <= 32))
            m_keyboardInfoText.text = StringForNonPrintable(c);
        else
            m_keyboardInfoText.text = c.ToString();
    }

    // callback function when a key is pressed on Keyboard
    private void KeyboardKeyPress(KeyControl control)
    {
        string keyName = control.keyCode.ToString();

        if (control.isPressed)
            StartKeyHightlight(keyName);
        else
            StopKeyHighlight(keyName);
    }

    // callback function when a button is pressed on Mouse
    private void MouseKeyPress(ButtonControl control)
    {
        string buttonName = control.name;

        if (control.isPressed)
            StartKeyHightlight(buttonName);
        else
            StopKeyHighlight(buttonName);
    }

    // callback function when a Vector2 value is passed in from mouse
    // including delta, position, radius, scroll and tilt.
    // Only delta and scroll have UI to reflect changes
    private void MouseVector2(Vector2Control control)
    {
        if (control.name == "delta")
            OnMouseDelta(control.ReadValue());
        else if (control.name == "scroll")
            OnMouseScroll(control.ReadValue());
    }

    private void OnMouseDelta(Vector2 move)
    {
        // Mouse move horizontally
        if (Mathf.Abs(move.x) > MOUSE_MOVE_DEADZONE)
        {
            if (move.x > 0)
            {
                StartMouseHighlight("Move Right");
                StopMouseHighlight("Move Left");
            }
            else
            {
                StartMouseHighlight("Move Left");
                StopMouseHighlight("Move Right");
            }
        }
        else
        {
            StopMouseHighlight("Move Right");
            StopMouseHighlight("Move Left");
        }

        // Mouse move vertically
        if (Mathf.Abs(move.y) > MOUSE_MOVE_DEADZONE)
        {
            if (move.y > 0)
            {
                StartMouseHighlight("Move Up");
                StopMouseHighlight("Move Down");
            }
            else
            {
                StartMouseHighlight("Move Down");
                StopMouseHighlight("Move Up");
            }
        }
        else
        {
            StopMouseHighlight("Move Up");
            StopMouseHighlight("Move Down");
        }
    }

    private void OnMouseScroll(Vector2 scroll)
    {
        // Only vertical scroll has UI. Horizontal scroll is shown in text box.
        if (scroll.y > 0)
        {
            StartMouseHighlight("Wheel Up");
            StopMouseHighlight("Wheel Down");
        }
        else if (scroll.y < 0)
        {
            StartMouseHighlight("Wheel Down");
            StopMouseHighlight("Wheel Up");
        }
        else
        {
            StopMouseHighlight("Wheel Up");
            StopMouseHighlight("Wheel Down");
        }
    }

    // Generate the red square over the key or mouse button
    private void StartKeyHightlight(string keyName)
    {
        Transform key = transform.Find("Keys/" + keyName);
        if (key == null)
            ShowMessage(keyName);
        else
        {
            SpriteRenderer sr = key.GetComponentInChildren<SpriteRenderer>();
            if (sr == null)
                Instantiate(m_keyHighlight, key.transform.position, key.transform.rotation, key);
            else
                sr.gameObject.SetActive(true);
        }
    }

    private void StopKeyHighlight(string keyName)
    {
        Transform key = transform.Find("Keys/" + keyName);
        if (key != null)
        {
            SpriteRenderer[] sr = key.GetComponentsInChildren<SpriteRenderer>();
            if (sr.Length > 0)
            {
                foreach (SpriteRenderer s in sr)
                    Destroy(s.gameObject);
            }
        }
    }

    private void StartMouseHighlight(string mouseAction)
    {
        Transform mAction = transform.Find("Mouse/" + mouseAction + "/Highlight_Arrow_Input_System");
        if (mAction != null)
            mAction.GetComponent<ArrowHighlight>().Play();
    }

    private void StopMouseHighlight(string mouseAction)
    {
        Transform mAction = transform.Find("Mouse/" + mouseAction + "/Highlight_Arrow_Input_System");
        if (mAction != null)
            mAction.GetComponent<ArrowHighlight>().Stop();
    }

    // Show the unmapped key name in the text field
    private void ShowMessage(string msg)
    {
        m_MessageWindow.text += "<color=red>" + msg + "</color>\n";
    }

    // From "KeyboardLastKey" by @Rene
    private String StringForNonPrintable(char ascii)
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
