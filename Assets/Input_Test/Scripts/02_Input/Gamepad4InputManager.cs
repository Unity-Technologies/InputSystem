using System;
using UnityEngine;
using UnityEngine.UI;

public class Gamepad4InputManager : MonoBehaviour
{
    public InputField unmapped_button_list;
    public ParticleSystem highlight_input_manager;

    // Update is called once per frame
    void Update()
    {
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
                StartHighlightButton(kcode);
            if (Input.GetKeyUp(kcode))
                StopHighlightButton(kcode);
        }

        // Only support the first 10 axles. Axles from 11th won't be able to show in this project
        for (int i = 1; i <= 10; i++)
        {
            string axisName = "Axis " + i;
            UpdateAxisValue(axisName);
        }
    }

    private void UpdateAxisValue(string axisName)
    {
        float value = Input.GetAxis(axisName);
        Transform axis = transform.Find("Input Manager/" + axisName);
        axis.GetComponent<TextMesh>().text = value.ToString("F2");
    }

    private void StartHighlightButton(KeyCode kcode)
    {
        string buttonCode = GetButtonCode(kcode);
        if (buttonCode == null)
            return;

        if (buttonCode != null)
        {
            StartHighlightButton(buttonCode);
            Debug.Log(buttonCode + " down");
        }
        else
            AddUnmappedButton(buttonCode);
    }

    private void StopHighlightButton(KeyCode kcode)
    {
        string buttonCode = GetButtonCode(kcode);
        if (buttonCode == null)
            return;

        if (buttonCode != null)
        {
            StopHighlightButton(buttonCode);
            Debug.Log(buttonCode + " up");
        }
    }

    private void StartHighlightButton(string buttonName)
    {
        Transform button = transform.Find("Input Manager/" + buttonName);
        ParticleSystem ps = button.GetComponentInChildren<ParticleSystem>();
        if (ps == null)
            Instantiate(highlight_input_manager, button.position - new Vector3(0f, 0f, 0.1f), button.rotation, button);
        else
            ps.Play();
    }

    private void StopHighlightButton(string buttonName)
    {
        Transform button = transform.Find("Input Manager/" + buttonName);
        ParticleSystem[] ps = button.GetComponentsInChildren<ParticleSystem>();
        if (ps.Length > 0)
        {
            foreach (ParticleSystem p in ps)
                p.Stop();
        }
    }

    // Remove "Joystick" from the key code value to find the button through code name
    private string GetButtonCode(KeyCode kcode)
    {
        string kcodeString = kcode.ToString();
        if (kcodeString.Contains("JoystickButton"))
            return kcodeString.Replace("Joystick", "");
        else
            return null;
    }

    private void AddUnmappedButton(string buttonName)
    {
        unmapped_button_list.text += "<color=blue>" + buttonName + "</color>\n";
    }
}