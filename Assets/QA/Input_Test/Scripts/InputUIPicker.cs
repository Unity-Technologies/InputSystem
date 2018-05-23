using UnityEngine;
using UnityEngine.UI;

public class InputUIPicker : MonoBehaviour
{
    // Input Gameobjects
    public GameObject mac_key_mouse;
    public GameObject windows_key_mouse;
    public GameObject xbox_controller;
    public GameObject generic_controller;
    public GameObject pen;

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
                SwitchToDiagram(xbox_controller);
                break;
            case 2:
                SwitchToDiagram(generic_controller);
                break;
            case 3:
                SwitchToDiagram(pen);
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
        SwitchToDiagram(mac_key_mouse);
#elif (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA)
        SwitchToDiagram(windows_key_mouse);
#else
        SwitchToDiagram(windows_key_mouse);
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
