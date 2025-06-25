using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Simple game manager that manages enabling/disabling in-game and UI actions.
    /// </summary>
    public class RebindUIGameManager : MonoBehaviour
    {
        public GameObject menu;
        public InputActionAsset gameplayActions;
        public InputActionReference menuAction;
        public InputActionReference exitMenuAction;

        private enum GameState
        {
            Initializing,
            Playing,
            RebindingMenu
        }

        private GameState m_CurrentState = GameState.Initializing;

        void Start()
        {
            SetState(GameState.Playing);

            // Let menu initially be disabled
            menu.SetActive(false);

            // Let gameplay actions be initially enabled
            gameplayActions.Enable();
        }

        private void SetState(GameState newState)
        {
            // Abort if there is no change to state
            if (newState == m_CurrentState)
                return;

            switch (newState)
            {
                // Entering game mode: enable in-game actions, show menu
                case GameState.Playing:
                    gameplayActions.Enable();
                    menu.SetActive(false);
                    break;

                // Entering menu: disable in-game actions, hide menu, make sure we have selection
                case GameState.RebindingMenu:
                    gameplayActions.Disable();
                    menu.SetActive(true);
                    if (EventSystem.current.currentSelectedGameObject == null)
                        EventSystem.current.SetSelectedGameObject(EventSystem.current.firstSelectedGameObject);
                    break;

                case GameState.Initializing:
                default:
                    break;
            }

            // Update current state
            m_CurrentState = newState;
        }

        private void OnMenu(InputAction.CallbackContext obj)
        {
            SetState(GameState.RebindingMenu);
        }

        private void OnExitMenu(InputAction.CallbackContext obj)
        {
            SetState(GameState.Playing);
        }

        private void OnEnable()
        {
            menuAction.action.performed += OnMenu;
            exitMenuAction.action.performed += OnExitMenu;
        }

        private void OnDisable()
        {
            menuAction.action.performed -= OnMenu;
            exitMenuAction.action.performed -= OnExitMenu;
        }
    }
}
