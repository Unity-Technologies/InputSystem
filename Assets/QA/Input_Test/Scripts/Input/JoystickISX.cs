using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class JoystickISX : MonoBehaviour
{
    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    private InputAction m_stickAction;

    // Use this for initialization
    void Start()
    {
        m_stickAction = new InputAction(name: "StickAction", binding: "<joystick>/<stick>");
        m_stickAction.performed += callbackContext => OnStickMove(callbackContext.control as StickControl);
        m_stickAction.canceled += callbackContext => OnStickMove(callbackContext.control as StickControl);
        m_stickAction.Enable();
    }

    private void OnStickMove(StickControl control)
    {
        Debug.Log("Stick Moved");
    }

    private void ShowMessage(string msg)
    {
        m_MessageWindow.text += "<color=red>" + msg + "</color>\n";
    }
}
