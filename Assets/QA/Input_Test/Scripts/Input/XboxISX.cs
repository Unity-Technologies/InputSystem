using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;

public class XboxISX : GamepadISX
{
    [Header("UI Element for Other Information")]
    public Text m_leftStickText;
    public Text m_rightStickText;

    private Gamepad m_xbox;

    // Use this for initialization
    void Start()
    {
        //m_stickMaxMove = 0.25f;

        m_buttonAction = new InputAction(name: "XboxButtonAction", binding: "XInputController*/<button>") { passThrough = true }; ;
        m_buttonAction.performed += callbackContext => OnControllerButtonPress(callbackContext.control as ButtonControl, isXbox: true);
        //m_buttonAction.cancelled += callbackContext => OnControllerButtonPress(callbackContext.control as ButtonControl, isXbox: true);
        m_buttonAction.Enable();

        m_dPadAction = new InputAction(name: "XboxDpadAction", binding: "XInputController*/<dpad>") { passThrough = true }; ;
        m_dPadAction.performed += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        //m_dPadAction.cancelled += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        m_dPadAction.Enable();

        m_stickMoveAction = new InputAction(name: "XboxStickMoveAction", binding: "XInputController*/<stick>") { passThrough = true }; ;
        m_stickMoveAction.performed += callbackContext => StickMove(callbackContext.control as StickControl);
        //m_stickMoveAction.cancelled += callbackContext => StickMove(callbackContext.control as StickControl);
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
        m_buttonAction.Disable();
        m_dPadAction.Disable();
        m_stickMoveAction.Disable();
    }

    private void Update()
    {
        if (m_xbox != null)
        {
            m_leftStickText.text = m_xbox.leftStick.ReadValue().ToString("F2");
            m_rightStickText.text = m_xbox.rightStick.ReadValue().ToString("F2");
        }

    }

    protected override void OnControllerButtonPress(ButtonControl control, string dpadName = null, bool isXbox = false, bool isPS = false)
    {
        base.OnControllerButtonPress(control, dpadName, isXbox, isPS);
        m_xbox = control.device as Gamepad;
    }

    protected override void OnDpadPress(DpadControl control)
    {
        base.OnDpadPress(control);
        m_xbox = control.device as Gamepad;
    }

    protected override void StickMove(StickControl control)
    {
        base.StickMove(control);
        m_xbox = control.device as Gamepad;
    }
}
