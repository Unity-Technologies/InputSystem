using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ProcessorFloatISX : ProcessorISX
{
    private float m_original = 0f;
    private float m_result = 0f;

    // Start is called before the first frame update
    void Start()
    {
        m_inputAction.Rename(gameObject.name);
        m_inputAction.performed += ctx => {
            InputControl<float> control = ctx.control as InputControl<float>;
            m_original = control.ReadValue();
            m_result = ctx.ReadValue<float>();
        };        
    }

    void OnEnable()
    {
        m_inputAction?.Enable();
    }

    void OnDisable()
    {
        m_inputAction?.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        m_originalText.text = m_original.ToString("F2");
        m_resultText.text = m_result.ToString("F2");
    }
}
