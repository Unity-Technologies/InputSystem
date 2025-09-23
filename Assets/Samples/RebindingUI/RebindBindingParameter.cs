using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;
using UnityEngine.UI;

public class RebindBindingParameter : MonoBehaviour
{
    /// <summary>
    /// Reference to the action that is to be rebound.
    /// </summary>
    public InputActionReference actionReference
    {
        get => m_Action;
        set
        {
            m_Action = value;
        }
    }

    [Tooltip("Reference to action that holds the parameter to be .")]
    [SerializeField]
    private InputActionReference m_Action;

    private Slider m_Slider;

    void Awake()
    {
        m_Slider = GetComponent<Slider>();
        // TODO How to we register an event listener?
    }

    public void ResetToDefault()
    {
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //m_Action.action.processors
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnEnable()
    {
        if (TryGetParameterValue(m_ParameterName, out var value))
            m_Slider.value = value;
    }

    private string m_ParameterName = "x";

    private bool TryGetParameterValue(string name, out float value)
    {
        if (m_Action != null)
        {
            //var k = m_Action.action.GetParameterValue((ScaleVector2Processor p) => p.x);

            var v = m_Action.action.GetParameterValue("x");
            if (v.HasValue)
            {
                var val = v.Value;
                if (val.type == TypeCode.Single)
                {
                    value = val.ToSingle();
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private void SetParameterValue(float value)
    {
        if (!Mathf.Approximately(m_Slider.value, value))
            m_Slider.value = value;
        if (m_Action != null)
            m_Action.action.ApplyParameterOverride(m_ParameterName, value);
    }

    private void UpdateDisplayValue()
    {
        if (m_Action == null) return;
        if (m_Action.action == null) return;
        var parameterValue = m_Action.action.GetParameterValue("x");
        if (parameterValue != null)
            m_Slider.value = parameterValue.Value.ToSingle();
    }
}
