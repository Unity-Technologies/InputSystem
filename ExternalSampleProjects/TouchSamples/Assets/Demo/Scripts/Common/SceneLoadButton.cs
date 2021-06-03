using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InputSamples.Demo
{
    /// <summary>
    /// Simple component to load a scene when a button is clicked.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SceneLoadButton : MonoBehaviour
    {
        [SerializeField]
        private string sceneName;

        private Button cachedButton;

        protected virtual void Awake()
        {
            cachedButton = GetComponent<Button>();
            cachedButton.onClick.AddListener(OnClick);
        }

        /// <summary>
        /// Perform scene loading.
        /// </summary>
        private void OnClick()
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
