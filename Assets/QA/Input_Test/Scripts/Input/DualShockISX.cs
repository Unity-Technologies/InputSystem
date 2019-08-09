using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class DualShockISX : GamepadISX
{
    [Header("UI Element for Other Information")]
    public Text m_leftStickText;
    public Text m_rightStickText;

    //private InputAction m_discreteButtonAction;

    // Start is called before the first frame update
    void Start()
    {
        m_buttonAction = new InputAction(name: "DualShockButtonAction", InputActionType.PassThrough,
            binding: "*DualShock*/<button>");
        m_buttonAction.performed += callbackContext => OnControllerButtonPress(callbackContext.control as ButtonControl, isPS: true);
        m_buttonAction.Enable();

        m_dPadAction = new InputAction(name: "DualShockDpadAction", InputActionType.PassThrough,
            binding: "*DualShock*/<dpad>");
        m_dPadAction.performed += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        m_dPadAction.Enable();

        m_stickMoveAction = new InputAction(name: "DualShockStickMoveAction", InputActionType.PassThrough,
            binding: "*DualShock*/<stick>");
        m_stickMoveAction.performed += callbackContext => StickMove(callbackContext.control as StickControl);
        m_stickMoveAction.Enable();
    }

    private void OnEnable()
    {
        if (m_buttonAction != null) m_buttonAction.Enable();
        if (m_dPadAction != null) m_dPadAction.Enable();
        if (m_stickMoveAction != null) m_stickMoveAction.Enable();
    }

    private void OnDisable()
    {
        m_buttonAction?.Disable();
        m_dPadAction?.Disable();
        m_stickMoveAction?.Disable();
    }

    private void Update()
    {
        Gamepad m_dualShock = Gamepad.current;
        if (m_dualShock != null)
        {
            m_leftStickText.text = m_dualShock.leftStick.ReadValue().ToString("F2");
            m_rightStickText.text = m_dualShock.rightStick.ReadValue().ToString("F2");
        }
    }
}
