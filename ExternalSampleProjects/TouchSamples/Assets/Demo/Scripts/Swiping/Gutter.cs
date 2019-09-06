using UnityEngine;

namespace InputSamples.Demo.Swiping
{
    /// <summary>
    /// Simple object to catch trash that falls off the screen.
    /// </summary>
    public class Gutter : MonoBehaviour
    {
        [SerializeField]
        private int gutterScore = -1;

        /// <summary>
        /// Found score controller in scene.
        /// </summary>
        private ScoringController cachedScoreController;

        private void Start()
        {
            cachedScoreController = ScoringController.Instance;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (cachedScoreController == null)
            {
                return;
            }

            // Try find trash on there
            var trash = other.GetComponent<Trash>();

            if (trash != null)
            {
                cachedScoreController.AddScore(gutterScore);
                Destroy(trash.gameObject);
            }
        }
    }
}
