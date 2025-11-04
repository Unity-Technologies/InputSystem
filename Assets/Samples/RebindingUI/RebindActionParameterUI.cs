using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
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
        /// The preference key to be used for persistence.
        /// </summary>
        public string preferenceKey
        {
            get => m_PreferenceKey;
            set => m_PreferenceKey = value;
        }

        /// <summary>
        /// The associated slider UI component instance.
        /// </summary>
        public Slider slider
        {
            get => m_Slider;
            set
            {
                if (m_Slider != null)
                    m_Slider.onValueChanged.RemoveListener(SetParameterValue);
                m_Slider = value;
                if (value != null)
                    value.onValueChanged.AddListener(SetParameterValue);
            }
        }

        /// <summary>
        /// The default value to apply when reset or no preference exist.
        /// </summary>
        public float defaultValue
        {
            get => m_DefaultValue;
            set => m_DefaultValue = value;
        }

        [Tooltip("Reference to action that holds the parameter to be configurable via this behaviour.")]
        [SerializeField]
        private InputActionReference m_Action;

        [Tooltip("Optional binding ID of the binding processor parameter to override.")]
        [SerializeField]
        private string m_BindingId;

        [Tooltip("The player preference key to be used for persistence.")]
        [SerializeField]
        private string m_PreferenceKey;

        [Tooltip("The default value to be be used when no preference exists or when resetting")]
        [SerializeField]
        private float m_DefaultValue;

        [Tooltip("The associated slider UI component used to change the value.")]
        [SerializeField]
        private Slider m_Slider;

        [SerializeField]
        private string[] m_ParameterOverrides;

        private float m_Value;

        public void ResetToDefault()
        {
            PlayerPrefs.SetFloat(m_PreferenceKey, m_DefaultValue);
            SetParameterValue(m_DefaultValue);
        }

        private void Awake()
        {
            if (m_Slider == null)
                m_Slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(m_PreferenceKey))
                SetParameterValue(PlayerPrefs.GetFloat(m_PreferenceKey, m_DefaultValue));

            if (m_Slider != null)
                m_Slider.onValueChanged.AddListener(SetParameterValue);
        }

        private void OnDisable()
        {
            if (m_Slider != null)
                m_Slider.onValueChanged.RemoveListener(SetParameterValue);

            if (!string.IsNullOrEmpty(m_PreferenceKey))
                PlayerPrefs.SetFloat(m_PreferenceKey, m_Value);
        }

        private void SetParameterValue(float value)
        {
            // Apply parameter value as a parametric override
            if (m_Action != null && m_Action.action != null)
            {
                var action = m_Action.action;
                int bindingIndex = action.FindBindingById(m_BindingId);
                var bindingMask = bindingIndex >= 0 ? action.bindings[bindingIndex] : default;

                // We apply parameter override. This directly affects matching processors and interactions
                // if they have matching parameters.
                foreach (var parameterOverride in m_ParameterOverrides)
                    action.ApplyParameterOverride(parameterOverride, value, bindingMask);
            }

            m_Value = value;

            UpdateDisplayValue(value);
        }

        private void UpdateDisplayValue(float value)
        {
            if (m_Slider != null)
                m_Slider.value = Mathf.Clamp(value, m_Slider.minValue, m_Slider.maxValue);
        }
    }
}
