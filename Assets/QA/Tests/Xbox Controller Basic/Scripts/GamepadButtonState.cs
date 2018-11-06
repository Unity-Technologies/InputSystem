using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.UI;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Plugins.XInput;

public class GamepadButtonState : MonoBehaviour
{
    public ButtonControl buttonToTrack;

    [Header("If left empty, will try to auto populate with GetComponent<Image>()")]
    public Image stateImage;

    private XInputController m_CurrentController;

    private Color m_RedTransparent;
    private Color m_WhiteTransparent;

    private void Start()
    {
        m_RedTransparent = new Color(1, 0, 0, 0.5f);
        m_WhiteTransparent = new Color(1, 1, 1, 0.1f);

        if (stateImage == null) { stateImage = GetComponent<Image>(); }
    }

    public void Update()
    {
        m_CurrentController = InputSystem.GetDevice<XInputController>();

        if (m_CurrentController == null) { return; }

        if (((ButtonControl)m_CurrentController[buttonToTrack.ToString()]).isPressed)
        {
            stateImage.color = m_RedTransparent;
        }
        else
        {
            stateImage.color = m_WhiteTransparent;
        }
    }
}
