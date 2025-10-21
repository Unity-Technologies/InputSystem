using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A UI to show messages reflecting changes to gameplay state.
    /// </summary>
    public class Message : MonoBehaviour
    {
        [Tooltip("The associated gameplay manager.")]
        public GameplayManager gameplayManager;

        [Tooltip("The associated UI root to hide/show.")]
        public GameObject root;

        [Tooltip("The associated UI text to be altered to show messages.")]
        public Text text;

        private Action m_TimeoutCallback;

        private void OnEnable()
        {
            // Monitor changes to gameplay state and game pause state.
            gameplayManager.GameplayStateChanged += OnGameplayStateChanged;
            gameplayManager.PauseChanged += OnPauseChanged;

            // Initialize
            OnGameplayStateChanged(gameplayManager.state);
        }

        private void OnDisable()
        {
            // Unsubscribe from monitoring gameplay and pause state.
            gameplayManager.GameplayStateChanged += OnGameplayStateChanged;
            gameplayManager.PauseChanged -= OnPauseChanged;
        }

        private void OnPauseChanged(bool paused)
        {
            OnGameplayStateChanged(gameplayManager.state);
        }

        private void Hide()
        {
            root.SetActive(false);
        }

        private void Show(string message)
        {
            text.text = message;
            root.SetActive(true);
        }

        private void Show(string message, float duration)
        {
            Show(message);
        }

        private void OnGameplayStateChanged(GameplayManager.GameplayState state)
        {
            if (gameplayManager.paused)
            {
                Show("PAUSED");
                return;
            }

            switch (state)
            {
                case GameplayManager.GameplayState.None:
                    break;
                case GameplayManager.GameplayState.StartLevel:
                    Show($"ROUND {gameplayManager.level}");
                    break;
                case GameplayManager.GameplayState.CompleteLevel:
                    break;
                case GameplayManager.GameplayState.Playing:
                    Hide();
                    break;
                case GameplayManager.GameplayState.GameOver:
                    Show("GAME OVER");
                    break;
            }
        }
    }
}
