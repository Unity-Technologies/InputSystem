using UnityEngine;

namespace InputSamples.Demo.Rolling
{
    /// <summary>
    /// Checkpoint for respawning.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem checkpointActiveParticles;

        /// <summary>
        /// Gets or sets the last checkpoint position.
        /// </summary>
        public static Vector3 LastCheckpoint { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            // If it's the ball, set our last checkpoint to this position
            var ball = other.GetComponentInParent<Ball>();
            if (ball == null)
            {
                return;
            }

            if (Vector3.SqrMagnitude(LastCheckpoint - transform.position) > Mathf.Epsilon)
            {
                LastCheckpoint = transform.position;
                if (checkpointActiveParticles != null)
                {
                    ParticleSystem.Burst spawnBurst = checkpointActiveParticles.emission.GetBurst(0);
                    int spawnCount = Random.Range(spawnBurst.minCount, spawnBurst.maxCount + 1);

                    checkpointActiveParticles.Emit(new ParticleSystem.EmitParams(), spawnCount);
                }
            }
        }
    }
}
