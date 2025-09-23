using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    [RequireComponent(typeof(Slider))]
    public class RebindActionParameterUI : MonoBehaviour
    {
        /// <summary>
        /// Reference to the action that is to be rebound (Required).
        /// </summary>
        public InputActionReference actionReference
        {
            get => m_Action;
            set => m_Action = value;
        }

        /// <summary>
        /// ID (in string form) of the binding that is to be rebound on the action.
        /// </summary>
        /// <remarks>If this is not set (null or empty), <see cref="parameterName"/> corresponds to an action processor
        /// parameter, otherwise it corresponds to a binding parameter.</remarks>
        /// <seealso cref="InputBinding.id"/>
        public string bindingId
        {
            get => m_BindingId;
            set => m_BindingId = value;
        }

        /// <summary>
        /// Parameter name of the parameter to be configured.
        /// </summary>
        /// <remarks>This corresponds to a binding processor parameter name when <see cref="bindingId"/> is not
        /// null nor empty and otherwise corresponds to an action processor parameter.</remarks>
        public string parameterName
        {
            get => m_ParameterName;
            set => m_ParameterName = value;
        }

        [Tooltip("Reference to action that holds the parameter to be configurable via this behaviour.")]
        [SerializeField]
        private InputActionReference m_Action;

        [Tooltip("Optional binding ID of the binding processor parameter to override.")]
        [SerializeField]
        private string m_BindingId;

        [Tooltip("The parameter name to be configured via this behaviour.")]
        [SerializeField]
        private string m_ParameterName;

        private Slider m_Slider;

        public void ResetToDefault()
        {
            if (m_Action != null && m_Action.action != null)
                m_Action.action.RemoveAllBindingOverrides();

            if (TryGetParameterValue(out var value))
                UpdateDisplayValue(value);
        }

        private void Awake()
        {
            m_Slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (TryGetParameterValue(out var value))
                UpdateDisplayValue(value);
            m_Slider.onValueChanged.AddListener(SetParameterValue);
        }

        private void OnDisable()
        {
            m_Slider.onValueChanged.RemoveListener(SetParameterValue);
        }

        private bool TryGetParameterValue(out float value)
        {
            if (m_Action != null && !string.IsNullOrEmpty(m_ParameterName))
            {
                //var k = m_Action.action.GetParameterValue((ScaleVector2Processor p) => p.x);

                var v = m_Action.action.GetParameterValue(m_ParameterName);
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
            // Apply parameter value as a parametric override
            if (m_Action != null && !string.IsNullOrEmpty(m_ParameterName))
            {
                m_Action.action.ApplyParameterOverride(m_ParameterName, value);
            }
                
            UpdateDisplayValue(value);
        }

        private void UpdateDisplayValue(float value)
        {
            if (m_Slider != null)
                m_Slider.value = Mathf.Clamp(value, m_Slider.minValue, m_Slider.maxValue);
        }
    }
}
