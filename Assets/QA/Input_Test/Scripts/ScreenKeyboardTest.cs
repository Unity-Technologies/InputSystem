using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input;


public class ScreenKeyboardTest : MonoBehaviour
{
    ScreenKeyboard m_ScreenKeyboard;
    // Start is called before the first frame update
    void Start()
    {
        m_ScreenKeyboard = InputSystem.AddDevice<AndroidScreenKeyboard>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Show()
    {
        m_ScreenKeyboard.Show();
    }

    public void Hide()
    {
        m_ScreenKeyboard.Hide();
    }
}
