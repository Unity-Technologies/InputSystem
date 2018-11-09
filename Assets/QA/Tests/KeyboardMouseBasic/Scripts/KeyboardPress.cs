using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.UI;

// This updates the color of a single image based on a specified Key enum.
// - Button not pressed - image is 0.1f transparent white
// - Button is pressed  - image color is shifted to half red
//
public class KeyboardPress : MonoBehaviour
{
    public Key reportKey;

    [Header("If left empty, will try to auto populate with GetComponent<Image>()")]
    public Image reportImage;

    private Keyboard m_Keyboard;

    private Color m_RedTransparent;
    private Color m_WhiteTransparent;

    private void Start()
    {
        m_RedTransparent = new Color(1, 0, 0, 0.5f);
        m_WhiteTransparent = new Color(1, 1, 1, 0.1f);

        if (reportImage == null) { reportImage = GetComponent<Image>(); }
    }

    void Update()
    {
        m_Keyboard = InputSystem.GetDevice<Keyboard>();

        if (m_Keyboard == null) { return; }

        if (m_Keyboard[reportKey].ReadValue() != 0)
        {
            reportImage.color = m_RedTransparent;
        }
        else
        {
            reportImage.color = m_WhiteTransparent;
        }
    }
}
