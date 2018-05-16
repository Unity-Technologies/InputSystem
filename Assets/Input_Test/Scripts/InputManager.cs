using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{

    // UI elements
    public Button btn_key_mouse;
    public Dropdown btn_others;

    // Input Gameobjects
    public GameObject mac_key_mouse;
    public GameObject windows_key_mouse;
    public GameObject xbox_controller;
    public GameObject generic_controller;
    public GameObject pen;

    // Define colors for the button and dropdown to keep the look uniform
    private Color col_background_enabled;
    private Color col_background_disabled;
    private Color col_text_enabled;
    private Color col_text_disabled;

    // Current displayed diagram
    private GameObject current;

    private void Start()
    {
        ColorUtility.TryParseHtmlString("#69D9F5FF", out col_background_enabled);
        ColorUtility.TryParseHtmlString("#7E7E7EFF", out col_background_disabled);
        ColorUtility.TryParseHtmlString("#323232FF", out col_text_enabled);
        ColorUtility.TryParseHtmlString("#626262FF", out col_text_disabled);

        ClickOnDropdown.clickDropdown += SwitchToController;

        SwitchToKeyMouse();
    }

    // Click button to show Keyboard & Mouse UI
    public void SwitchToKeyMouse()
    {
        // change color
        ColorBlock cb = btn_key_mouse.colors;

        cb.normalColor = cb.highlightedColor = col_background_enabled;
        btn_key_mouse.colors = cb;
        btn_key_mouse.transform.GetComponentInChildren<Text>().color = col_text_enabled;

        cb.normalColor = cb.highlightedColor = col_background_disabled;
        btn_others.colors = cb;
        btn_others.captionText.color = col_text_disabled;

#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
        SwitchToDiagram(mac_key_mouse);
#elif (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA)
        SwitchToDiagram(windows_key_mouse);
#else
        SwitchToDiagram(windows_key_mouse);
#endif

    }

    // Pick from the dropdown list to show controller UI
    public void SwitchToController()
    {
        // change color
        ColorBlock cb = btn_key_mouse.colors;

        cb.normalColor = cb.highlightedColor = col_background_disabled;
        btn_key_mouse.colors = cb;
        btn_key_mouse.transform.GetComponentInChildren<Text>().color = col_text_disabled;

        cb.normalColor = cb.highlightedColor = col_background_enabled;
        btn_others.colors = cb;
        btn_others.captionText.color = col_text_enabled;

        int value = btn_others.value;
        if (value == 0)
            SwitchToDiagram(generic_controller);
        else if (value == 1)
            SwitchToDiagram(xbox_controller);
        else if (value == 2)
            SwitchToDiagram(pen);
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
