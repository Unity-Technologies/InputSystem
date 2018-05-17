using UnityEngine;

public class MouseKeyboardPicker : MonoBehaviour
{
    // Input Gameobjects
    public GameObject mac_key_mouse;
    public GameObject windows_key_mouse;

    // Use this for initialization
    void Start()
    {
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
        mac_key_mouse.SetActive(true);
#else
        windows_key_mouse.SetActive(true);
#endif
    }
}
