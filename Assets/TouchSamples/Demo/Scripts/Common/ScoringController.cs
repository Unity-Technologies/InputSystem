using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace InputSamples.Demo
{
    public class ScoringController : Singleton<ScoringController>
    {
        // Scoring label.
        [SerializeField]
        private Text scoreLabel;

        private float score;

        public void AddScore(float newScore)
        {
            score += newScore;

            if (scoreLabel != null)
            {
                scoreLabel.text = Mathf.RoundToInt(score).ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
