using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    public class Message : MonoBehaviour
    {
        public GameplayManager gameplayManager;
        public GameObject root;
        public Text text;
        private Action m_TimeoutCallback;

        private void OnEnable()
        {
            gameplayManager.GameplayStateChanged += OnGameplayStateChanged;
            gameplayManager.PauseChanged += OnPauseChanged;
            OnGameplayStateChanged(gameplayManager.state);
        }

        private void OnPauseChanged(bool paused)
        {
            OnGameplayStateChanged(gameplayManager.state);
        }

        private void OnDisable()
        {
            gameplayManager.GameplayStateChanged += OnGameplayStateChanged;
            gameplayManager.PauseChanged -= OnPauseChanged;
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
                    Debug.Log("Starting level");
                    Show($"ROUND {gameplayManager.level}");
                    break;
                case GameplayManager.GameplayState.CompleteLevel:
                    break;
                case GameplayManager.GameplayState.Playing:
                    Debug.Log("Playing level");
                    Hide();
                    break;
                case GameplayManager.GameplayState.GameOver:
                    Show("GAME OVER");
                    break;
            }
        }
    }
}
