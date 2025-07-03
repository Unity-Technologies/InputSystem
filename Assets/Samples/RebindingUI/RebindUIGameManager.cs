using System;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

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

        [Tooltip("The actions asset that holds Gameplay, Common and UI action maps to be used. (Required).")]
        public InputActionAsset actions;

        [Tooltip("Whether UI actions should be disabled during gameplay.")]
        public bool enableUIActionsDuringGameplay = true;

        [Tooltip("The gameplay manager responsible for managing gameplay.")]
        public GameplayManager gameplayManager;

        [Tooltip("The gameplay UI")]
        public GameObject gameUI;

        private GameState m_CurrentState = GameState.Initializing;
        private GameState m_NextState = GameState.Playing;

        private InputActionMap gameplayActions;
        private InputActionMap uiActions;
        private InputAction toggleMenuAction;

        private void Awake()
        {
            gameplayActions = actions.FindActionMap("Gameplay");
            uiActions = actions.FindActionMap("UI");
            toggleMenuAction = actions.FindAction("Common/Menu");
        }

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
                    // Only allow transition back to the game if game menu is interactable.
                    // This is to avoid e.g. pressing menu toggle action while in rebind mode.
                    // Essentially this is equivalent "if NOT currently rebinding".
                    if (menu.GetComponent<CanvasGroup>().interactable)
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

        private void OnToggleMenu(InputAction.CallbackContext obj)
        {
            ToggleMenu();
        }

        private void OnEnable()
        {
            toggleMenuAction.performed += OnToggleMenu;
            toggleMenuAction.Enable();
        }

        private void OnDisable()
        {
            toggleMenuAction.performed -= OnToggleMenu;
            toggleMenuAction.Disable();
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
                    gameplayManager.enabled = true;
                    if (enableUIActionsDuringGameplay)
                        uiActions.Enable();
                    else
                        uiActions.Disable();

                    gameUI.SetActive(true);
                    menu.SetActive(false);
                    break;

                // Entering menu: disable in-game actions, hide menu, make sure we have selection.
                // Also make sure or toggle menu action is enabled in case its part of gameplay actions.
                case GameState.RebindingMenu:
                    gameplayActions.Disable();
                    gameplayManager.enabled = false;
                    if (!enableUIActionsDuringGameplay)
                        uiActions.Enable();

                    gameUI.SetActive(false);
                    menu.SetActive(true);

                    // Workaround: Make sure we always have a select game object since Unity UI might otherwise show
                    // without a selection which might prevent gamepad navigation.
                    var eventSystem = EventSystem.current;
                    if (eventSystem.currentSelectedGameObject == null)
                        eventSystem.SetSelectedGameObject(eventSystem.firstSelectedGameObject);
                    break;
            }
        }
    }
}
