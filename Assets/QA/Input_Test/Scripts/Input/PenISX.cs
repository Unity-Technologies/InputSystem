using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;

public class PenISX : MonoBehaviour
{
    [Tooltip("Highlight for Pen Input")]
    public ParticleSystem m_highlightPS;

    [Tooltip("Sign for Out of Range")]
    public GameObject m_outOfRangeSign;

    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    [Header("UI Elements for Debug Info")]
    public TextMesh m_pressureText;
    public Text m_penInfoText;

    private InputAction m_penAction;

    private const float HORIZONTAL_RANGE = 8f;
    private const float VERTICAL_RANGE = 2.7f;

    private Transform pen_holder;
    private Transform pen_rotation;
    private Vector3 original_pos;
    private Vector3 rotation_adjust;

    private bool is_pen_rotating = false;

    // Use this for initialization
    void Start()
    {
        pen_holder = transform.Find("Pen");
        if (pen_holder == null)
            throw new Exception("Gameobject \"Pen\" is not found!");

        pen_rotation = pen_holder.Find("RotationHolder");

        original_pos = pen_holder.position;
        rotation_adjust = pen_rotation.GetChild(0).localEulerAngles;

        m_penAction = new InputAction(name: "PenButtonAction", binding: "<pen>/<button>");
        m_penAction.performed += callbackContext => ButtonPress(callbackContext.control as ButtonControl);
        m_penAction.cancelled += callbackContext => ButtonPress(callbackContext.control as ButtonControl);
        m_penAction.Enable();
    }

    void OnEnable()
    {
        if (m_penAction != null)
            m_penAction.Enable();
    }

    private void OnDisable()
    {
        m_penAction.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        Pen pen = InputSystem.GetDevice<Pen>();
        if (pen == null) return;

        // Update position
        Vector2 pos = pen.position.ReadValue();
        pen_holder.position = original_pos + new Vector3(pos.x * HORIZONTAL_RANGE / Screen.width,
            pos.y * VERTICAL_RANGE / Screen.height, 0);

        // Update tilt
        Vector2 tilt = pen.tilt.ReadValue();
        pen_rotation.localEulerAngles = new Vector3(tilt.y, 0, tilt.x) * -90;

        // Update twist if available
        float twist = pen.twist.ReadValue();
        pen_rotation.GetChild(0).localEulerAngles = rotation_adjust + new Vector3(0, twist * -360, 0);

        // Update ISX information text UI
        m_penInfoText.text = pen.phase.ReadValue().ToString() + "\n"
            + pos.ToString("F0") + "\n"
            + tilt.ToString("F2") + "\n"
            + twist.ToString("F2");

        // Update pressure indicator
        float pressure = pen.pressure.ReadValue();
        Color newColor = Color.red;
        newColor.a = pressure;
        m_pressureText.color = newColor;
        m_pressureText.text = "Pressure: " + pressure.ToString("F2");

        // Update inRange state/indicator
        m_outOfRangeSign.SetActive(!pen.inRange.isPressed);
    }

    private void ButtonPress(ButtonControl control)
    {
        string buttonName = control.name;
        if (buttonName == "tip" || buttonName == "eraser")
        {
            if (control.ReadValue() > 0)
            {
                pen_rotation.position -= new Vector3(0, 0.2f, 0);
                if (buttonName == "tip")
                    StartRotatePen(0);
                else
                    StartRotatePen(180);
                m_highlightPS.Play();
            }
            else
            {
                pen_rotation.position += new Vector3(0, 0.2f, 0);
                StartRotatePen(0);
                m_highlightPS.Stop();
            }
        }
        // Any other button is listed in the Input Name list
        else if (buttonName != "inRange")
        {
            string str = buttonName + ((control.ReadValue() == 0) ? " released" : " pressed");
            ShowMessage(str);
        }
    }

    private void StartRotatePen(int target_angel)
    {
        if (Mathf.Abs(rotation_adjust.z - target_angel) < 1)
            return;

        if (is_pen_rotating)
            StopCoroutine("RotatePen");
        StartCoroutine("RotatePen", target_angel);
    }

    private IEnumerator RotatePen(int target_angel)
    {
        is_pen_rotating = true;
        float step = (target_angel - rotation_adjust.z) * 0.2f;
        while (Mathf.Abs(rotation_adjust.z - target_angel) > 1)
        {
            rotation_adjust.z += step;
            yield return new WaitForEndOfFrame();
        }
        is_pen_rotating = false;
    }

    private string FirstLetterToUpper(string str)
    {
        if (String.IsNullOrEmpty(str))
            return null;
        else if (str.Length == 1)
            return str.ToUpper();
        else
            return char.ToUpper(str[0]) + str.Substring(1);
    }

    private void ShowMessage(string msg)
    {
        m_MessageWindow.text += "<color=brown>" + msg + "</color>\n";
    }
}
