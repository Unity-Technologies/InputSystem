using UnityEngine;

namespace InputSamples.Demo.Swiping
{
    /// <summary>
    /// A bin that can catch the trash.
    /// </summary>
    public class Bin : MonoBehaviour
    {
        [SerializeField]
        private TrashType trashType;
        [SerializeField]
        private int correctScore = 1;
        [SerializeField]
        private int incorrectScore = -2;

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
                cachedScoreController.AddScore(trash.Type == trashType ? correctScore : incorrectScore);
                Destroy(trash.gameObject);
            }
        }
    }
}
