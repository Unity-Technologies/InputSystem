using System;
using UnityEngine;
using UnityEngine.InputSystem;
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

        [Tooltip("The duration for which the indicator should be lit before becoming completely inactive.")]
        public float duration = 1.0f;

        private double m_RealTimeLastPerformed;
        private Image m_Image;

        void Awake()
        {
            m_Image = GetComponent<Image>();
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
            var elapsedSincePerformed = Time.realtimeSinceStartupAsDouble - m_RealTimeLastPerformed;
            m_Image.color = duration <= 0.0f
                ? inactiveColor
                : Color.Lerp(inactiveColor, activeColor,
                (float)Math.Max(0.0, 1.0 - elapsedSincePerformed / duration));
        }
    }
}
