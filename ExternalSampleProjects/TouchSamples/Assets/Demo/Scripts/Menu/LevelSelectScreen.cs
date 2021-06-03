using UnityEngine;
using UnityEngine.SceneManagement;

namespace InputSamples.Demo.Menu
{
    /// <summary>
    /// Level select screen.
    /// </summary>
    public class LevelSelectScreen : MonoBehaviour
    {
        /// <summary>
        /// The first level item in the list. It will be cloned to create more items.
        /// </summary>
        [Tooltip("The first level item in the list. It will be cloned to create more items."), SerializeField]
        private LevelSelectItem firstLevelItem;

        private void Start()
        {
            BuildListOfLevels();
        }

        /// <summary>
        /// Build the list of levels.
        /// </summary>
        private void BuildListOfLevels()
        {
            // Get the list of scenes in the build settings
            var reuseFirstItem = true;
            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0, len = SceneManager.sceneCountInBuildSettings; i < len; i++)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));

                // Skip the active scene
                if (activeScene.name == fileName)
                {
                    continue;
                }

                LevelSelectItem item;
                if (reuseFirstItem)
                {
                    // Re-use the first item
                    item = firstLevelItem;
                    reuseFirstItem = false;
                }
                else
                {
                    // Clone the first item
                    item = Instantiate(firstLevelItem);
                    item.transform.SetParent(firstLevelItem.transform.parent, false);
                }

                item.Initialize(fileName);
            }
        }
    }
}
