using UnityEngine;

namespace InputSamples.Demo.Tapping
{
    /// <summary>
    /// Simple manager to handle playing particle effects for the Tapping game. Reuses a single
    /// system for all effects.
    /// </summary>
    public class Effects : Singleton<Effects>
    {
        [SerializeField]
        private ParticleSystem hitParticles;
        [SerializeField]
        private ParticleSystem missParticles;
        [SerializeField]
        private ParticleSystem timeoutParticles;

        public void PlayHitParticles(Vector3 position)
        {
            PlayParticles(position, hitParticles);
        }

        public void PlayMissParticles(Vector3 position)
        {
            PlayParticles(position, missParticles);
        }

        public void PlayTimeoutParticles(Vector3 position)
        {
            PlayParticles(position, timeoutParticles);
        }

        /// <summary>
        /// Play particles at the given position.
        /// </summary>
        private void PlayParticles(Vector3 position, ParticleSystem selectedParticle)
        {
            ParticleSystem.Burst spawnBurst = selectedParticle.emission.GetBurst(0);
            int spawnCount = Random.Range(spawnBurst.minCount, spawnBurst.maxCount + 1);

            position.z = selectedParticle.transform.position.z;
            selectedParticle.transform.position = position;
            selectedParticle.Emit(new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true
            }, spawnCount);
        }
    }
}
