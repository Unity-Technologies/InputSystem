using System;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardMouse4InputManager : MonoBehaviour
{
    public ParticleSystem highlight_key_input_manager;
    public InputField unmapped_key_list;
    public Text mouse_pos_text;

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
        float wheel = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(moveX) > 0.5)
        {
            if (moveX > 0)
            {
                StartMouseHighlight("Move_Right");
                StopMouseHighlight("Move_Left");
            }
            else
            {
                StartMouseHighlight("Move_Left");
                StopMouseHighlight("Move_Right");
            }
        }
        else
        {
            StopMouseHighlight("Move_Left");
            StopMouseHighlight("Move_Right");
        }

        if (Mathf.Abs(moveY) > 0.5)
        {
            if (moveY > 0)
            {
                StartMouseHighlight("Move_Up");
                StopMouseHighlight("Move_Down");
            }
            else
            {
                StartMouseHighlight("Move_Down");
                StopMouseHighlight("Move_Up");
            }
        }
        else
        {
            StopMouseHighlight("Move_Up");
            StopMouseHighlight("Move_Down");
        }

        // Mouse wheel
        if (wheel > 0)
        {
            StartMouseHighlight("Wheel_Up");
            StopMouseHighlight("Wheel_Down");
        }
        else if (wheel < 0)
        {
            StartMouseHighlight("Wheel_Down");
            StopMouseHighlight("Wheel_Up");
        }
        else
        {
            StopMouseHighlight("Wheel_Up");
            StopMouseHighlight("Wheel_Down");
        }

        // Update mouse position
        mouse_pos_text.text = Input.mousePosition.ToString("F0");
    }

    // Generate the blue ring Particle System over the key or mouse button
    private void StartKeyHighlight(string keyName)
    {
        Transform key = transform.Find("Keys/" + keyName);

        if (key == null)
            AddUnmappedKey(keyName);
        else
        {
            ParticleSystem ps = key.GetComponentInChildren<ParticleSystem>();
            if (ps == null)
                Instantiate(highlight_key_input_manager, key.position, key.rotation, key);
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
        Transform mAction = transform.Find("Mouse/" + mouseAction);

        if (mAction != null)
            mAction.GetComponentInChildren<ArrowHighlight>().Play();
    }

    // Stop the arrow highlight
    private void StopMouseHighlight(string mouseAction)
    {
        Transform mAction = transform.Find("Mouse/" + mouseAction);

        if (mAction != null)
            mAction.GetComponentInChildren<ArrowHighlight>().Stop();
    }

    // Show the unmapped key name in the text field
    private void AddUnmappedKey(string keyName)
    {
        unmapped_key_list.text += "<color=blue>" + keyName + "</color>\n";
    }
}
