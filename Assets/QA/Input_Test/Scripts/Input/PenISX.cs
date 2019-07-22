using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PenISX : MonoBehaviour
{
    [Tooltip("Highlight for Pen Input")]
    public ParticleSystem m_highlightPS;

    [Tooltip("Sign for Out of Range")]
    public GameObject m_outOfRangeSign;

    [Header("Info UI")]
    public TextMesh m_pressureText;
    public Text m_penInfoText;

    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    private InputAction m_penButtonAction;
    private InputAction m_penVector2Action;
    private InputAction m_penAxisAction;

    private const float HORIZONTAL_RANGE = 8f;
    private const float VERTICAL_RANGE = 2.7f;

    private Transform pen_holder;
    private Transform pen_rotation;
    private Vector3 m_originalPos;
    private Vector3 m_rotateAdjust;

    private bool m_isRotating = false;

    // Use this for initialization
    void Start()
    {
        pen_holder = transform.Find("Pen");
        if (pen_holder == null)
            throw new Exception("Gameobject \"Pen\" is not found!");

        pen_rotation = pen_holder.Find("RotationHolder");

        m_originalPos = pen_holder.position;
        m_rotateAdjust = pen_rotation.GetChild(0).localEulerAngles;

        m_penButtonAction =
            new InputAction(name: "PenButtonAction", InputActionType.PassThrough, binding: "<pen>/<button>");
        m_penButtonAction.performed += callbackContext => ButtonPress(callbackContext.control as ButtonControl);
        //m_penAction.cancelled += callbackContext => ButtonPress(callbackContext.control as ButtonControl);
        m_penButtonAction.Enable();

        m_penVector2Action = new InputAction(name: "PenVectorAction", InputActionType.PassThrough, binding: "<pen>/<vector2>");
        m_penVector2Action.performed += callbackContext => OnVector2Change(callbackContext.control as Vector2Control);
        m_penVector2Action.Enable();

        m_penAxisAction = new InputAction(name: "PenAxisAction", InputActionType.PassThrough, binding: "<pen>/twist");
        m_penAxisAction.AddBinding("<pen>/pressure");
        m_penAxisAction.performed += callbackContext => OnAxisChange(callbackContext.control as AxisControl);
        m_penAxisAction.Enable();
    }

    void OnEnable()
    {
        m_penButtonAction?.Enable();
        m_penVector2Action?.Enable();
        m_penAxisAction?.Enable();
    }

    private void OnDisable()
    {
        m_penButtonAction?.Disable();
        m_penVector2Action?.Disable();
        m_penAxisAction?.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        Pen pen = InputSystem.GetDevice<Pen>();
        if (pen == null) return;

        // Update ISX information text UI
        m_penInfoText.text = pen.position.ReadValue().ToString("F0") + "\n"
            + pen.tilt.ReadValue().ToString("F2") + "\n"
            + pen.twist.ReadValue().ToString("F2") + "\n"
            + pen.delta.ReadValue().ToString("F2");

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
        switch (control.name)
        {
            case "tip":
            case "eraser":
                if (control.ReadValue() > 0)
                {
                    pen_rotation.position -= new Vector3(0, 0.2f, 0);
                    if (control.name == "tip")
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
                break;

            case "inRange":
                m_outOfRangeSign.SetActive(control.ReadValue() == 0);
                break;

            case "button":
                break;

            default:
                string str = control.name + ((control.ReadValue() == 0) ? " released" : " pressed");
                ShowMessage(str);
                break;
        }
    }

    // Update visual element for Position and tilt
    private void OnVector2Change(Vector2Control control)
    {
        if (control.name == "position")
            pen_holder.position = new Vector3(control.ReadValue().x * HORIZONTAL_RANGE / Screen.width, control.ReadValue().y * VERTICAL_RANGE / Screen.height, 0) + m_originalPos;
        else if (control.name == "tilt")
            pen_rotation.localEulerAngles = new Vector3(control.ReadValue().y, 0, control.ReadValue().x) * -90;
    }

    // Update visual element for twist and pressue
    private void OnAxisChange(AxisControl control)
    {
        if (control.name == "twist")
            pen_rotation.GetChild(0).localEulerAngles = m_rotateAdjust + new Vector3(0, control.ReadValue() * -360, 0);

        else if (control.name == "pressure")
        {
            Color newColor = Color.red;
            newColor.a = control.ReadValue();
            var main = m_highlightPS.main;
            main.startColor = newColor;
            m_pressureText.color = newColor;
            m_pressureText.text = "Pressure: " + control.ReadValue().ToString("F2");
        }
    }

    private void StartRotatePen(int targetAngle)
    {
        if (Mathf.Abs(m_rotateAdjust.z - targetAngle) < 1)
            return;

        if (m_isRotating)
            StopCoroutine("RotatePen");
        StartCoroutine("RotatePen", targetAngle);
    }

    private IEnumerator RotatePen(int targetAngle)
    {
        m_isRotating = true;
        float step = (targetAngle - m_rotateAdjust.z) * 0.2f;
        while (Mathf.Abs(m_rotateAdjust.z - targetAngle) > 1)
        {
            m_rotateAdjust.z += step;
            pen_rotation.GetChild(0).localEulerAngles = m_rotateAdjust;
            yield return new WaitForEndOfFrame();
        }
        m_isRotating = false;
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
