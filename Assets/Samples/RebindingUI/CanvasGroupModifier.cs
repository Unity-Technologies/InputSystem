using UnityEngine.EventSystems;

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
        private GameObject m_SelectedObject;

        private void OnEnable()
        {
            if (canvasGroup != null)
            {
                // Store selection to make sure it is not changed when switching "windows".
                m_SelectedObject = EventSystem.current.currentSelectedGameObject;

                // Save current setting and override
                m_SavedInteractable = canvasGroup.interactable;
                canvasGroup.interactable = interactable;
            }
        }

        private void OnDisable()
        {
            if (canvasGroup != null)
            {
                // Restore previous setting.
                canvasGroup.interactable = m_SavedInteractable;

                // Restore previous selection.
                var eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    if (m_SelectedObject != null)
                        eventSystem.SetSelectedGameObject(m_SelectedObject);
                    else if (EventSystem.current.currentSelectedGameObject == null)
                        eventSystem.SetSelectedGameObject(eventSystem.firstSelectedGameObject);
                }
            }
        }
    }
}
