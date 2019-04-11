using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;

public class InputUIPicker : MonoBehaviour
{
    public Dropdown m_inputPickerDropdown;

    [Header("Input Gameobject")]
    public GameObject m_windowsKeyboardMouse;
    public GameObject m_macKeyboardMouse;
    public GameObject m_controllerDiagram;
    public GameObject m_xboxController;
    public GameObject m_dualShockController;
    public GameObject m_joystick;
    public GameObject m_pen;
    public GameObject m_touch;

    //[Header("Input Action")]
    //public InputAction m_switchToKeyboardMouseAction;
    //public InputAction m_switchToXboxAction;
    //public InputAction m_switchToGamepadDiagramAction;
    //public InputAction m_switchToJoystickAction;
    //public InputAction m_switchToPenAction;
    //public InputAction m_switchToTouchAction;

    // Current displayed diagram
    private GameObject m_currentDisplay;

    void Start()
    {
        SwitchToKeyMouse();

        m_inputPickerDropdown.onValueChanged.RemoveAllListeners();
        m_inputPickerDropdown.onValueChanged.AddListener(delegate { SwitchToInputMethod(m_inputPickerDropdown); });

        //m_switchToKeyboardMouseAction.performed += _ => SwitchToInputMethod(0);
        //m_switchToXboxAction.performed += _ => SwitchToInputMethod(1);
        //m_switchToGamepadDiagramAction.performed += _ => SwitchToInputMethod(2);
        //m_switchToJoystickAction.performed += _ => SwitchToInputMethod(3);
        //m_switchToPenAction.performed += _ => SwitchToInputMethod(4);
        //m_switchToTouchAction.performed += _ => SwitchToInputMethod(5);
    }

    //void OnEnable()
    //{
    //    m_switchToKeyboardMouseAction.Enable();
    //    m_switchToXboxAction.Enable();
    //    m_switchToGamepadDiagramAction.Enable();
    //    m_switchToJoystickAction.Enable();
    //    m_switchToPenAction.Enable();
    //    m_switchToTouchAction.Enable();
    //}

    //void OnDisable()
    //{
    //    m_switchToKeyboardMouseAction.Disable();
    //    m_switchToXboxAction.Disable();
    //    m_switchToGamepadDiagramAction.Disable();
    //    m_switchToJoystickAction.Disable();
    //    m_switchToPenAction.Disable();
    //    m_switchToTouchAction.Disable();
    //}

    // !!!!!TEMPORARY: Before composite input is implemented
    void Update()
    {
        if (InputSystem.GetDevice<Keyboard>() == null) return;

        Keyboard currentKeyboard = InputSystem.GetDevice<Keyboard>();
        if (currentKeyboard.leftCtrlKey.isPressed || currentKeyboard.rightCtrlKey.isPressed)
        {
            if (currentKeyboard.digit1Key.isPressed)
                m_inputPickerDropdown.value = 0;
            else if (currentKeyboard.digit2Key.isPressed)
                m_inputPickerDropdown.value = 1;
            else if (currentKeyboard.digit3Key.isPressed)
                m_inputPickerDropdown.value = 2;
            else if (currentKeyboard.digit4Key.isPressed)
                m_inputPickerDropdown.value = 3;
            else if (currentKeyboard.digit5Key.isPressed)
                m_inputPickerDropdown.value = 4;
            else if (currentKeyboard.digit6Key.isPressed)
                m_inputPickerDropdown.value = 5;
            else if (currentKeyboard.digit7Key.isPressed)
                m_inputPickerDropdown.value = 6;
        }
    }

    private void SwitchToInputMethod(Dropdown picker)
    {
        SwitchToInputMethod(Convert.ToByte(picker.value));
    }

    private void SwitchToInputMethod(byte inputValue)
    {
        // Debug.Log("Switch to Input: " + inputValue);
        switch (inputValue)
        {
            case 1:
                SwitchToDiagram(m_xboxController);
                break;
            case 2:
                SwitchToDiagram(m_dualShockController);
                break;
            case 3:
                SwitchToDiagram(m_controllerDiagram);
                break;
            case 4:
                SwitchToDiagram(m_joystick);
                break;
            case 5:
                SwitchToDiagram(m_pen);
                break;
            case 6:
                SwitchToDiagram(m_touch);
                break;
            case 0:
            default:
                SwitchToKeyMouse();
                break;
        }
    }

    private void SwitchToKeyMouse()
    {
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
        SwitchToDiagram(m_macKeyboardMouse);
#elif (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA)
        SwitchToDiagram(m_windowsKeyboardMouse);
#else
        SwitchToDiagram(m_windowsKeyboardMouse);
#endif
    }

    private void SwitchToDiagram(GameObject newDiagram)
    {
        if (m_currentDisplay != newDiagram)
        {
            if (m_currentDisplay != null)
                m_currentDisplay.SetActive(false);
            m_currentDisplay = newDiagram;
            m_currentDisplay.SetActive(true);
        }
    }
}
