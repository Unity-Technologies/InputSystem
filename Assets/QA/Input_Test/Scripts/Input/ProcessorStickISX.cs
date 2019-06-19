using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ProcessorStickISX : ProcessorISX
{
    [Header("Other UI element")]
    public Image m_stickImage;
    public int m_stickImageOffsetFactor = 10;

    private Vector2 m_unprocessed = new Vector2(0, 0);
    private Vector2 m_result = new Vector2(0, 0);

    // Start is called before the first frame update
    void Start()
    {
        m_inputAction.Rename(gameObject.name);
        m_inputAction.performed += ctx => {
            AxisControl control = ctx.control as AxisControl;
            m_unprocessed.x = control.ReadUnprocessedValue();
            m_result.x = control.ReadValue();
        };
        m_inputAction.canceled += ctx => {
            m_unprocessed = m_result = new Vector2(0, 0);
        };
    }

    private void OnEnable()
    {
        m_inputAction.Enable();
    }

    private void OnDisable()
    {
        m_inputAction?.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        m_stickImage.transform.localPosition = m_unprocessed * m_stickImageOffsetFactor;
        m_unprocessedText.text = m_unprocessed.ToString("F2");
        m_resultText.text = m_result.ToString("F2");
    }
}
