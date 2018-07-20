using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;

public class InputUIPicker : MonoBehaviour
{
    // Input Gameobjects
    public GameObject m_windowsKeyboardMouse;
    public GameObject m_macKeyboardMouse;
    public GameObject m_controllerDiagram;
    public GameObject m_xboxController;
    public GameObject m_pen;
    public GameObject m_touch;

    public InputAction m_switchToKeyboardMouseAction;
    public InputAction m_switchToXboxAction;
    public InputAction m_switchToGamepadDiagramAction;
    public InputAction m_switchToPenAction;
    public InputAction m_switchToTouchAction;

    // Current displayed diagram
    private GameObject m_currentDisplay;
    private Keyboard m_currentKeyboard;

    void Start()
    {
        SwitchToKeyMouse();
        m_currentKeyboard = Keyboard.current;
        //m_switchToKeyboardMouseAction.performed += _ => SwitchToInputMethod(0);
        //m_switchToXboxAction.performed += _ => SwitchToInputMethod(1);
        //m_switchToGamepadDiagramAction.performed += _ => SwitchToInputMethod(2);
        //m_switchToPenAction.performed += _ => SwitchToInputMethod(3);
        //m_switchToTouchAction.performed += _ => SwitchToInputMethod(4);
    }

    void OnEnable()
    {
        //m_switchToKeyboardMouseAction.Enable();
        //m_switchToXboxAction.Enable();
        //m_switchToGamepadDiagramAction.Enable();
        //m_switchToPenAction.Enable();
        //m_switchToTouchAction.Enable();
    }

    void OnDisable()
    {
        m_switchToKeyboardMouseAction.Disable();
        m_switchToXboxAction.Disable();
        m_switchToGamepadDiagramAction.Disable();
        m_switchToPenAction.Disable();
        m_switchToTouchAction.Disable();
    }

    // !!!!!TEMPORARY: Before composite input is implemented
    void Update()
    {
        if (m_currentKeyboard.leftCtrlKey.isPressed || m_currentKeyboard.rightCtrlKey.isPressed)
        {
            if (m_currentKeyboard.digit1Key.isPressed)
                SwitchToInputMethod(0);
            else if (m_currentKeyboard.digit2Key.isPressed)
                SwitchToInputMethod(1);
            else if (m_currentKeyboard.digit3Key.isPressed)
                SwitchToInputMethod(2);
            else if (m_currentKeyboard.digit4Key.isPressed)
                SwitchToInputMethod(3);
            else if (m_currentKeyboard.digit5Key.isPressed)
                SwitchToInputMethod(4);
        }
    }

    public void SwitchToInputMethod(Dropdown picker)
    {
        SwitchToInputMethod(Convert.ToByte(picker.value));
    }

    private void SwitchToInputMethod(byte inputValue)
    {
        Debug.Log("Switch to Input: " + inputValue);
        switch (inputValue)
        {
            case 1:
                SwitchToDiagram(m_xboxController);
                break;
            case 2:
                SwitchToDiagram(m_controllerDiagram);
                break;
            case 3:
                SwitchToDiagram(m_pen);
                break;
            case 4:
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
