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

        public Image performedIndicator;
        public Image pressedIndicator;
        public Text label;

        private double m_RealTimeLastPerformed;

        private void OnEnable()
        {
            if (action != null && action.action != null)
            {
                action.action.performed += OnPerformed;
            }
        }

        private void OnDisable()
        {
            if (action != null && action.action != null)
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
                // Pulse active color if enabled and performed
                var elapsedSincePerformed = Time.realtimeSinceStartupAsDouble - m_RealTimeLastPerformed;
                if (performedIndicator)
                {
                    performedIndicator.color = duration <= 0.0f
                        ? inactiveColor
                        : Color.Lerp(inactiveColor, activeColor,
                        (float)Math.Max(0.0, 1.0 - elapsedSincePerformed / duration));
                }

                if (pressedIndicator)
                    pressedIndicator.color = action.action.IsPressed() ? activeColor : inactiveColor;
            }
            else
            {
                // Show disabled indicator if disabled
                if (performedIndicator && performedIndicator.color != disabledColor)
                    performedIndicator.color = disabledColor;
                if (pressedIndicator && pressedIndicator.color != disabledColor)
                    pressedIndicator.color = disabledColor;
            }
        }

        // Also update action label in edit-mode
#if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateActionLabel();
        }

#endif

        private void UpdateActionLabel()
        {
            if (label == null)
                return;
            if (action != null && action.action != null)
                label.text = action.action.name;
            else
                label.text = string.Empty;
        }
    }
}
