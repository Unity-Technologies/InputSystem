using System;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardMouseOldInput : MonoBehaviour
{
    [Tooltip("Highlight Prefab")]
    public ParticleSystem m_keyHighlight;

    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    [Header("UI Elements for Debug Info")]
    public Text m_keyboardInfoText;
    public Text m_mouseInfoText;

#if ENABLE_LEGACY_INPUT_MANAGER
    void Update()
    {
        // Keyboard input or mouse button is pressed
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
                StartKeyHighlight(kcode.ToString());

            if (Input.GetKeyUp(kcode))
                StopKeyHighlight(kcode.ToString());
        }

        // Mouse move
        float moveX = Input.GetAxis("Mouse X");
        float moveY = Input.GetAxis("Mouse Y");
        float wheel = Input.mouseScrollDelta.y;

        if (Mathf.Abs(moveX) > 0.5)
        {
            if (moveX > 0)
            {
                StartMouseHighlight("Move Right");
                StopMouseHighlight("Move Left");
            }
            else
            {
                StartMouseHighlight("Move Left");
                StopMouseHighlight("Move Right");
            }
        }
        else
        {
            StopMouseHighlight("Move Left");
            StopMouseHighlight("Move Right");
        }

        if (Mathf.Abs(moveY) > 0.5)
        {
            if (moveY > 0)
            {
                StartMouseHighlight("Move Up");
                StopMouseHighlight("Move Down");
            }
            else
            {
                StartMouseHighlight("Move Down");
                StopMouseHighlight("Move Up");
            }
        }
        else
        {
            StopMouseHighlight("Move Up");
            StopMouseHighlight("Move Down");
        }

        // Mouse wheel
        if (wheel > 0)
        {
            StartMouseHighlight("Wheel Up");
            StopMouseHighlight("Wheel Down");
        }
        else if (wheel < 0)
        {
            StartMouseHighlight("Wheel Down");
            StopMouseHighlight("Wheel Up");
        }
        else
        {
            StopMouseHighlight("Wheel Up");
            StopMouseHighlight("Wheel Down");
        }

        // Update debug mouse info
        if (!String.IsNullOrEmpty(Input.inputString))
            m_keyboardInfoText.text = Input.inputString;
        m_mouseInfoText.text = Input.mousePosition.ToString("F0") + "\n"
            + Input.mouseScrollDelta.ToString("F0") + "\n"
            + "(" + moveX.ToString("F2") + " ," + moveY.ToString("F2") + ")";
    }

    // Generate the blue ring Particle System over the key or mouse button
    private void StartKeyHighlight(string keyName)
    {
        Transform key = transform.Find("Keys/" + keyName);

        if (key == null)
            ShowMessage(keyName);
        else
        {
            ParticleSystem ps = key.GetComponentInChildren<ParticleSystem>();
            if (ps == null)
                Instantiate(m_keyHighlight, key.position, key.rotation, key);
            else
                ps.Play();
        }
    }

    // Stop the Particle System for keys and mouse buttons
    private void StopKeyHighlight(string keyName)
    {
        Transform key = transform.Find("Keys/" + keyName);

        if (key != null)
        {
            ParticleSystem[] ps = key.GetComponentsInChildren<ParticleSystem>();
            if (ps.Length > 0)
            {
                foreach (ParticleSystem p in ps)
                    p.Stop();
            }
        }
    }

    // Generate the blue arrow for move movement and wheel
    private void StartMouseHighlight(string mouseAction)
    {
        Transform mAction = transform.Find("Mouse/" + mouseAction + "/Highlight_Arrow_Input_Manager");

        if (mAction != null)
            mAction.GetComponent<ArrowHighlight>().Play();
    }

    // Stop the arrow highlight
    private void StopMouseHighlight(string mouseAction)
    {
        Transform mAction = transform.Find("Mouse/" + mouseAction + "/Highlight_Arrow_Input_Manager");

        if (mAction != null)
            mAction.GetComponent<ArrowHighlight>().Stop();
    }

    // Show the unmapped key name in the text field
    private void ShowMessage(string msg)
    {
        m_MessageWindow.text += "<color=blue>" + msg + "</color>\n";
    }

#endif
}
