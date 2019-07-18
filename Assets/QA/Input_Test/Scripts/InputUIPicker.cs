using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InputUIPicker : MonoBehaviour
{
    public Dropdown m_deviceDropdown;
    public Dropdown m_otherDropdown;

    [Header("Device Test GameObject")]
    public GameObject m_windowsKeyboardMouse;
    public GameObject m_macKeyboardMouse;
    public GameObject m_controllerDiagram;
    public GameObject m_xboxController;
    public GameObject m_dualShockController;
    public GameObject m_joystick;
    public GameObject m_pen;
    public GameObject m_touch;

    [Header("Other Test GameObject")]
    public GameObject m_interactions;
    public GameObject m_processors;

    // Current displayed diagram
    private GameObject m_currentDisplay;

    void Start()
    {
        SwitchToKeyMouse();
    }

    void Update()
    {
        // !!!!!TEMPORARY: Only Shortcut for Old Input Manager
        if (InputSystem.GetDevice<Keyboard>() == null) return;

        Keyboard currentKeyboard = InputSystem.GetDevice<Keyboard>();
        if (currentKeyboard.leftCtrlKey.isPressed || currentKeyboard.rightCtrlKey.isPressed)
        {
            if (currentKeyboard.digit1Key.isPressed)
                m_deviceDropdown.value = 1;
            else if (currentKeyboard.digit2Key.isPressed)
                m_deviceDropdown.value = 2;
            else if (currentKeyboard.digit3Key.isPressed)
                m_deviceDropdown.value = 3;
            else if (currentKeyboard.digit4Key.isPressed)
                m_deviceDropdown.value = 4;
            else if (currentKeyboard.digit5Key.isPressed)
                m_deviceDropdown.value = 5;
            else if (currentKeyboard.digit6Key.isPressed)
                m_deviceDropdown.value = 6;
            else if (currentKeyboard.digit7Key.isPressed)
                m_deviceDropdown.value = 7;
        }
    }

    public void SwitchToDeviceTest(int value)
    {
        switch (value)
        {
            case 1:
                SwitchToKeyMouse();
                break;
            case 2:
                SwitchToTestObject(m_xboxController);
                break;
            case 3:
                SwitchToTestObject(m_dualShockController);
                break;
            case 4:
                SwitchToTestObject(m_controllerDiagram);
                break;
            case 5:
                SwitchToTestObject(m_joystick);
                break;
            case 6:
                SwitchToTestObject(m_pen);
                break;
            case 7:
                SwitchToTestObject(m_touch);
                break;
            default:
                break;
        }
        m_otherDropdown.value = 0;
    }

    public void SwitchToOtherTest(int value)
    {
        if (value == 1)
            SwitchToTestObject(m_interactions);
        else if (value == 2)
            SwitchToTestObject(m_processors);
        m_deviceDropdown.value = 0;
    }

    private void SwitchToKeyMouse()
    {
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
        SwitchToTestObject(m_macKeyboardMouse);
#elif (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA)
        SwitchToTestObject(m_windowsKeyboardMouse);
#else
        SwitchToTestObject(m_windowsKeyboardMouse);
#endif
    }

    private void SwitchToTestObject(GameObject newDiagram)
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
