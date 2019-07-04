using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InputSamples.Demo.Menu
{
    /// <summary>
    /// A level item on the level select screen.
    /// </summary>
    public class LevelSelectItem : MonoBehaviour
    {
        // Level name text.
        [Tooltip("Level name text label"), SerializeField]
        private Text nameText;

        /// <summary>
        /// Initialize the item.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        public void Initialize(string sceneName)
        {
            nameText.text = sceneName;
        }

        /// <summary>
        /// Clicked the button.
        /// </summary>
        public void OnClickButton()
        {
            SceneManager.LoadScene(nameText.text);
        }
    }
}
