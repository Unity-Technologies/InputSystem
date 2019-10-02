using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class XboxISX : GamepadISX
{
    [Header("UI Element for Other Information")]
    public Text m_leftStickText;
    public Text m_rightStickText;

    // Use this for initialization
    void Start()
    {
        //m_stickMaxMove = 0.25f;

        m_buttonAction = new InputAction(name: "XboxButtonAction", InputActionType.PassThrough, binding: "<XInputController>/<button>");
        m_buttonAction.performed += callbackContext => OnControllerButtonPress(callbackContext.control as ButtonControl, isXbox: true);
        m_buttonAction.Enable();

        m_dPadAction = new InputAction(name: "XboxDpadAction", InputActionType.PassThrough, binding: "<XInputController>/<dpad>");
        m_dPadAction.performed += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        m_dPadAction.Enable();

        m_stickMoveAction = new InputAction(name: "XboxStickMoveAction", InputActionType.PassThrough,
            binding: "<XInputController>/<stick>");
        m_stickMoveAction.performed += callbackContext => StickMove(callbackContext.control as StickControl);
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

    private void Update()
    {
        Gamepad m_xbox = Gamepad.current;
        if (m_xbox != null)
        {
            m_leftStickText.text = m_xbox.leftStick.ReadValue().ToString("F2");
            m_rightStickText.text = m_xbox.rightStick.ReadValue().ToString("F2");
        }
    }
}
