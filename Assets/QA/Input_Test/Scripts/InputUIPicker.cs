using UnityEngine;
using UnityEngine.UI;

public class InputUIPicker : MonoBehaviour
{
    // Input Gameobjects
    public GameObject m_windowsKeyboardMouse;
    public GameObject m_macKeyboardMouse;
    public GameObject m_controllerDiagram;
    public GameObject m_xboxController;
    public GameObject m_pen;
    public GameObject m_touch;

    // Current displayed diagram
    private GameObject current;

    private void Start()
    {
        SwitchToKeyMouse();
    }

    public void SwitchToInputMethod(Dropdown picker)
    {
        switch (picker.value)
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
        if (current != newDiagram)
        {
            if (current != null)
                current.SetActive(false);
            current = newDiagram;
            current.SetActive(true);
        }
    }
}
