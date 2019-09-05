using System.Linq;
using UnityEngine;

namespace InputSamples.Demo.Tapping
{
    /// <summary>
    /// Component to spawn tap zones.
    /// </summary>
    public class SpawnController : MonoBehaviour
    {
        /// <summary>
        /// Prefab to spawn.
        /// </summary>
        [SerializeField]
        private TapTarget targetPrefab;

        /// <summary>
        /// Shortest time interval that spawns can happen at.
        /// </summary>
        [SerializeField]
        private float shortestInterval;

        /// <summary>
        /// Weighting factors for selection of multiples of <see cref="shortestInterval"/> for the next
        /// spawn. Higher numbers mean that multiple happens more often.
        /// </summary>
        [SerializeField]
        private int[] intervalMultiplierWeights;

        /// <summary>
        /// Weighted factors for selection of the number of spawns to make at every attempt.
        /// </summary>
        [SerializeField]
        private int[] spawnCountWeights;

        /// <summary>
        /// Turning angle for rotational spawning.
        /// </summary>
        [SerializeField]
        private float turnAngle;

        /// <summary>
        /// Near threshold for how far from the middle to spawn.
        /// </summary>
        [SerializeField]
        private float minCenterDist;

        /// <summary>
        /// Top threshold for how far from the middle to spawn.
        /// </summary>
        [SerializeField]
        private float maxCenterDist;

        private int intervalWeightSum, spawnCountWeightSum;

        private float nextSpawnTime;

        private Vector2 spawnVector;
        private Quaternion advancingRotation;

        protected virtual void Awake()
        {
            // Precalculate all spawn weights
            intervalWeightSum = intervalMultiplierWeights.Sum();
            spawnCountWeightSum = spawnCountWeights.Sum();

            // Calculate the first random spawn vector
            // For some reason there is no Random.onUnitCircle, to our utter dismay.
            float randomAngle = Random.Range(0.0f, Mathf.PI * 2);
            spawnVector = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));

            // Precalculate quaternion that will rotate our spawn angle every spawn attempt
            // Randomly flip turn angle
            turnAngle *= Random.value < 0.5f ? 1.0f : -1.0f;

            advancingRotation = Quaternion.AngleAxis(turnAngle, Vector3.forward);
        }

        protected virtual void Start()
        {
            nextSpawnTime = Time.realtimeSinceStartup + targetPrefab.ShrinkTime;

            // Initial spawn
            Spawn();
        }

        private void Spawn()
        {
            int spawns = GetWeightedSpawnCount();
            for (var i = 0; i < spawns; ++i)
            {
                SpawnNextTarget();
            }

            nextSpawnTime += shortestInterval * GetWeightedSpawnInterval();

            Invoke(nameof(Spawn), nextSpawnTime - Time.realtimeSinceStartup - targetPrefab.ShrinkTime);
        }

        private void SpawnNextTarget()
        {
            TapTarget spawnedObject = Instantiate(targetPrefab);

            Vector2 spawnPosition = transform.TransformDirection(spawnVector) * Random.Range(minCenterDist, maxCenterDist);

            spawnedObject.transform.position = spawnPosition;
            spawnedObject.Initialize(nextSpawnTime);

            // Advance spawn vector
            spawnVector = advancingRotation * spawnVector;
        }

        private int GetWeightedSpawnInterval()
        {
            var intervalMultiplier = 0;
            int randomIntervalSelect = Random.Range(0, intervalWeightSum);
            while (intervalMultiplier < intervalMultiplierWeights.Length &&
                   intervalMultiplierWeights[intervalMultiplier] < randomIntervalSelect)
            {
                randomIntervalSelect -= intervalMultiplierWeights[intervalMultiplier];
                intervalMultiplier++;
            }

            return intervalMultiplier + 1;
        }

        private int GetWeightedSpawnCount()
        {
            var spawnCount = 0;
            int randomSpawnCountSelect = Random.Range(0, spawnCountWeightSum);
            while (spawnCount < spawnCountWeights.Length &&
                   spawnCountWeights[spawnCount] < randomSpawnCountSelect)
            {
                randomSpawnCountSelect -= spawnCountWeights[spawnCount];
                spawnCount++;
            }

            return spawnCount + 1;
        }
    }
}
