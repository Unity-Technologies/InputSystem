namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Simple utility that modifies a referenced CanvasGroup while being active.
    /// </summary>
    public class CanvasGroupModifier : MonoBehaviour
    {
        [Tooltip("The Canvas Group to be modified while this component is active")]
        public CanvasGroup canvasGroup;

        [Tooltip("The interactable setting to use for the Canvas Group while this component is active")]
        public bool interactable = false;

        private bool m_SavedInteractable;

        void OnEnable()
        {
            if (canvasGroup != null)
            {
                // Save current setting and override
                m_SavedInteractable = canvasGroup.interactable;
                canvasGroup.interactable = interactable;
            }
        }

        void OnDisable()
        {
            if (canvasGroup != null)
            {
                // Restore previous setting
                canvasGroup.interactable = m_SavedInteractable;
            }
        }
    }
}
