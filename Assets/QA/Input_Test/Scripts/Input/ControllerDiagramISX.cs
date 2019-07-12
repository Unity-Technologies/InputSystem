using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ControllerDiagramISX : GamepadISX
{
    // Use this for initialization
    void Start()
    {
        m_buttonAction = new InputAction(name: "ButtonPressAction", InputActionType.PassThrough, binding: "*/<button>");
        m_buttonAction.performed += callbackContext => OnButtonPress(callbackContext.control as ButtonControl);
        m_buttonAction.canceled += callbackContext => OnButtonPress(callbackContext.control as ButtonControl);
        m_buttonAction.Enable();

        m_dPadAction = new InputAction(name: "Dpadpressaction", InputActionType.PassThrough, binding: "*/<dpad>");
        m_dPadAction.performed += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        m_dPadAction.canceled += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        m_dPadAction.Enable();

        m_stickMoveAction = new InputAction(name: "StickMoveAction", InputActionType.PassThrough, binding: "*/<stick>");
        m_stickMoveAction.performed += callbackContext => StickMove(callbackContext.control as StickControl);
        m_stickMoveAction.canceled += callbackContext => StickMove(callbackContext.control as StickControl);
        m_stickMoveAction.Enable();
    }

    private void OnEnable()
    {
        if (m_buttonAction != null)     m_buttonAction.Enable();
        if (m_dPadAction != null)       m_dPadAction.Enable();
        if (m_stickMoveAction != null)  m_stickMoveAction.Enable();
    }

    private void OnDisable()
    {
        m_buttonAction?.Disable();
        m_dPadAction?.Disable();
        m_stickMoveAction?.Disable();
    }

    // Callback funtion when a button is pressed. The button can be on a keyboard or mouse
    private void OnButtonPress(ButtonControl control)
    {
        // Rule out Keyboard and Mouse input
        string device = control.device.description.deviceClass;
        if (device == "Keyboard" || device == "Mouse")
            return;

        OnControllerButtonPress(control);
    }

    // Get the Transform in scene for input control (button, stick, dpad)
    // If no existing one is assigned, assign a new one
    protected override Transform GetInputTransform(string inputName, bool isStick = false, string dpadName = null)
    {
        bool isDpad = (dpadName == null) ? false : true;
        Transform input = isDpad ? m_buttonContainer.Find(dpadName) : m_buttonContainer.Find(inputName);
        // First time use
        if (input == null)
        {
            if (isStick)     input = m_buttonContainer.Find("Gamepad Stick");
            else if (isDpad) input = m_buttonContainer.Find("Gamepad Dpad");
            else             input = m_buttonContainer.Find("Gamepad Button");

            // if unassigned Gameobject ran out. highly unlikely, but in case
            if (input == null)
            {
                ShowMessage(inputName);
                return null;
            }
            else
            {
                input.name = isDpad ? dpadName : inputName;
                FirstTimeUse(input);
            }
        }
        if (isStick)        input = input.Find("Stick");
        else if (isDpad)    input = input.Find(inputName);
        return input;
    }

    protected override void StickMove(StickControl control)
    {
        Transform stick = GetInputTransform(FirstLetterToUpper(control.name), isStick: true);
        Vector2 pos = control.ReadValue();
        if (stick != null)
            stick.localPosition = new Vector3(pos.x * m_stickMaxMove, pos.y * m_stickMaxMove, stick.localPosition.z);

        // update the text
        Transform positionText = stick.parent.Find("Pos");
        if (positionText != null)
            positionText.GetComponent<TextMesh>().text = pos.ToString("F2");
    }

    // When a input is used for the first time, remove all tranparency from it
    private void FirstTimeUse(Transform controlTrans)
    {
        // Remove transparency from all the Sprite Renderers
        foreach (SpriteRenderer sr in controlTrans.GetComponentsInChildren<SpriteRenderer>())
            sr.color = RemoveColorTranparency(sr.color);

        // Remove transparency from the text mesh and change text to the transform's name
        foreach (TextMesh tm in controlTrans.GetComponentsInChildren<TextMesh>())
        {
            tm.color = RemoveColorTranparency(tm.color);
            if (tm.name == "Name")
                tm.text = controlTrans.name;
        }
    }

    private Color RemoveColorTranparency(Color color)
    {
        color.a = 1f;
        return color;
    }
}
