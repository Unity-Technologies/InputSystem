using UnityEngine;

namespace InputSamples.Demo.Tapping
{
    /// <summary>
    /// The target the user will try to tap on. These have a narrow tap window when they are safe to tap.
    /// </summary>
    public class TapTarget : MonoBehaviour
    {
        [SerializeField]
        private Color activeColor;

        [SerializeField]
        private Color inactiveColor;

        [SerializeField]
        private float hitScore = 10;

        [SerializeField]
        private float hitTimeThreshold = 0.15f;

        [SerializeField]
        private float missScore = -7;

        [SerializeField]
        private float ignoreScore = -5;

        [SerializeField]
        private float shrinkTime = 2.0f;

        [SerializeField]
        private Gradient colors;

        [SerializeField]
        private SpriteRenderer timerObject;

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        private float startingScale;
        private float hitTime;
        private float invThreshold;

        public float ShrinkTime => shrinkTime;

        protected virtual void Awake()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = colors.Evaluate(Random.value);
            }

            startingScale = timerObject.transform.localScale.x;
            invThreshold = 1 / hitTimeThreshold;

            timerObject.color = inactiveColor;
        }

        protected virtual void Update()
        {
            SetSize();
        }

        public void Initialize(float hitTime)
        {
            this.hitTime = hitTime;
            SetSize();
        }

        public void Hit(float timeStamp)
        {
            float normalizedThresholdTime = GetNormalizedHitTime(timeStamp);

            if (normalizedThresholdTime >= 1)
            {
                OnMiss(missScore, false);
            }
            else
            {
                OnHit(1 - normalizedThresholdTime);
            }
        }

        private void OnHit(float normalizedHitTime)
        {
            // Hit!
            ScoringController sc;
            if (ScoringController.TryGetInstance(out sc))
            {
                sc.AddScore(normalizedHitTime * normalizedHitTime * hitScore);
            }

            Effects effects;
            if (Effects.TryGetInstance(out effects))
            {
                effects.PlayHitParticles(transform.position);
            }

            Destroy(gameObject);
        }

        private void OnMiss(float score, bool timeout)
        {
            // Miss!
            ScoringController sc;
            if (ScoringController.TryGetInstance(out sc))
            {
                sc.AddScore(score);
            }

            Effects effects;
            if (Effects.TryGetInstance(out effects))
            {
                if (!timeout)
                {
                    //it's a miss - tapped too soon - red particle effect
                    effects.PlayMissParticles(transform.position);
                }
                else
                {
                    // it's a timeout, didn't tap in time, particle disapears in white smoke
                    effects.PlayTimeoutParticles(transform.position);
                }
            }

            Destroy(gameObject);
        }

        protected void SetSize()
        {
            float startTime = hitTime - shrinkTime;
            float normalizedProgress = Mathf.Clamp01(Mathf.InverseLerp(startTime, hitTime, Time.realtimeSinceStartup));

            timerObject.transform.localScale = Vector3.one *
                Mathf.Lerp(startingScale, 1.0f, normalizedProgress);

            if (normalizedProgress >= 1)
            {
                if (Time.realtimeSinceStartup - hitTime >= hitTimeThreshold)
                {
                    // call on miss with True for Timeout, Timeout effect is played
                    OnMiss(ignoreScore, true);
                }

                // Shrink down
                float normalizedEnd = (Time.realtimeSinceStartup - hitTime) * invThreshold;
                transform.localScale = Vector3.one * (1 - normalizedEnd);
            }

            // If we're within our safe time, change the timer to safe color
            float normalizedHitTime = GetNormalizedHitTime(Time.realtimeSinceStartup);
            if (normalizedHitTime < 1)
            {
                timerObject.color = activeColor;
            }
        }

        /// <summary>
        /// Returns a normalized value for how far we're within our tap window
        /// </summary>
        /// <returns>
        /// 0 if the tap is exactly at the target time, 1 at the edge of the window on each side, and
        /// > 1 if outside the tap window
        /// </returns>
        private float GetNormalizedHitTime(float timeStamp)
        {
            return Mathf.Abs(timeStamp - hitTime) * invThreshold;
        }
    }
}
