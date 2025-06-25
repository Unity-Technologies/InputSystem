using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Simple game manager that manages enabling/disabling in-game and UI actions.
    /// </summary>
    /// <remarks>State transitions happens per frame and hence handles throttling.</remarks>
    public class RebindUIGameManager : MonoBehaviour
    {
        [Tooltip("The in-game menu object to be activated and deactivated when menu is toggled (Required).")]
        public GameObject menu;

        [Tooltip("The gameplay actions to be disabled when exiting game mode and enabled when entering game mode (Required).")]
        public InputActionAsset gameplayActions;

        [Tooltip("The input action to be used to toggle menu (Required).")]
        public InputActionReference toggleMenuAction;

        /// <summary>
        /// Toggles between game state and rebinding menu state.
        /// </summary>
        public void ToggleMenu()
        {
            switch (m_CurrentState)
            {
                case GameState.Playing:
                    m_NextState = GameState.RebindingMenu;
                    break;
                case GameState.RebindingMenu:
                    m_NextState = GameState.Playing;
                    break;
            }
        }

        private enum GameState
        {
            Initializing,
            Playing,
            RebindingMenu
        }

        private GameState m_CurrentState = GameState.Initializing;
        private GameState m_NextState = GameState.Playing;

        private void OnToggleMenu(InputAction.CallbackContext obj)
        {
            ToggleMenu();
        }

        private void OnEnable()
        {
            toggleMenuAction.action.performed += OnToggleMenu;
        }

        private void OnDisable()
        {
            toggleMenuAction.action.performed -= OnToggleMenu;
        }

        private void Update()
        {
            // Abort if there is no change to state
            if (m_CurrentState == m_NextState)
                return;

            // Update current state
            m_CurrentState = m_NextState;

            // Handle state transition
            switch (m_NextState)
            {
                // Entering game mode: enable in-game actions, show menu
                case GameState.Playing:
                    gameplayActions.Enable();
                    menu.SetActive(false);
                    break;

                // Entering menu: disable in-game actions, hide menu, make sure we have selection.
                // Also make sure or toggle menu action is enabled in case its part of gameplay actions.
                case GameState.RebindingMenu:
                    gameplayActions.Disable();
                    toggleMenuAction.action.Enable();
                    menu.SetActive(true);
                    if (EventSystem.current.currentSelectedGameObject == null)
                        EventSystem.current.SetSelectedGameObject(EventSystem.current.firstSelectedGameObject);
                    break;
            }
        }
    }
}
