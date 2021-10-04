using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

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
            binding: "*Dual*/<button>");
        m_buttonAction.performed += callbackContext => OnControllerButtonPress(callbackContext.control as ButtonControl, isPS: true);
        m_buttonAction.Enable();

        m_dPadAction = new InputAction(name: "DualShockDpadAction", InputActionType.PassThrough,
            binding: "*Dual*/<dpad>");
        m_dPadAction.performed += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        m_dPadAction.Enable();

        m_stickMoveAction = new InputAction(name: "DualShockStickMoveAction", InputActionType.PassThrough,
            binding: "*Dual*/<stick>");
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

    private void OnGUI()
    {
        if (!(Gamepad.current is DualShockGamepad))
            return;

        var gamepad = Gamepad.current as DualShockGamepad;

        if (GUILayout.Button("red"))
            gamepad.SetLightBarColor(Color.red);
        if (GUILayout.Button("green"))
            gamepad.SetLightBarColor(Color.green);
        if (GUILayout.Button("black"))
            gamepad.SetLightBarColor(Color.black);
        if (GUILayout.Button("1,1"))
            gamepad.SetMotorSpeeds(1.0f, 1.0f);
        if (GUILayout.Button("1,0"))
            gamepad.SetMotorSpeeds(1.0f, 0.0f);
        if (GUILayout.Button("0,1"))
            gamepad.SetMotorSpeeds(0.0f, 1.0f);
        if (GUILayout.Button("0.5,0.5"))
            gamepad.SetMotorSpeeds(0.5f, 0.5f);
        if (GUILayout.Button("0.5,0.0"))
            gamepad.SetMotorSpeeds(0.5f, 0.0f);
        if (GUILayout.Button("0.0,0.5"))
            gamepad.SetMotorSpeeds(0.0f, 0.5f);
        if (GUILayout.Button("0,0"))
            gamepad.SetMotorSpeeds(0.0f, 0.0f);
    }
}
