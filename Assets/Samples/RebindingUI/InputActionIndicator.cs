using System;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A simple visual indicator of action performed.
    /// </summary>
    /// <remarks>Error handling have been excluded for simplicity.</remarks>
    [RequireComponent(typeof(Image))]
    public class InputActionIndicator : MonoBehaviour
    {
        [Tooltip("Reference to the associated action to be visualized.")]
        public InputActionReference action;

        [Tooltip("The color to show when the associated action is performed.")]
        public Color activeColor = Color.green;

        [Tooltip("The color to show when the associated action has not been performed for the specified duration.")]
        public Color inactiveColor = Color.black;
        
        [Tooltip("The color to show when the associated action is disabled")]
        public Color disabledColor = Color.red;

        [Tooltip("The duration for which the indicator should be lit before becoming completely inactive.")]
        public float duration = 1.0f;

        private double m_RealTimeLastPerformed;
        private Image m_Image;
        private Text m_Text;

        void Awake()
        {
            m_Image = GetComponent<Image>();
            m_Text = GetComponent<Text>();
            Update();
        }

        private void OnEnable()
        {
            action.action.performed += OnPerformed;
            action.action.Enable();
        }

        private void OnDisable()
        {
            action.action.Disable();
            action.action.performed -= OnPerformed;
        }

        private void OnPerformed(InputAction.CallbackContext obj)
        {
            m_RealTimeLastPerformed = Time.realtimeSinceStartupAsDouble;
        }

        private void Update()
        {
            if (action.action.enabled)
            {
                // Pulse active color if enabled
                var elapsedSincePerformed = Time.realtimeSinceStartupAsDouble - m_RealTimeLastPerformed;
                m_Image.color = duration <= 0.0f
                    ? inactiveColor
                    : Color.Lerp(inactiveColor, activeColor,
                        (float)Math.Max(0.0, 1.0 - elapsedSincePerformed / duration));
            }
            else
            {
                // Show disabled indicator if disabled
                if (m_Image.color != disabledColor)
                    m_Image.color = disabledColor;
            }
        }
        
        // We want the label for the action name to update in edit mode, too, so
        // we kick that off from here.
#if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateActionLabel();
        }
#endif
        
        private void UpdateActionLabel()
        {
            if (m_Text == null) 
                return;
            if (action != null && action.action != null)
                m_Text.text = action.name;
            else
                m_Text.text = string.Empty;
        }
    }
}
