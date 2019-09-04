using UnityEngine;

namespace InputSamples.Demo.Swiping
{
    /// <summary>
    /// Component to spawn trash to fall from the top of the screen.
    /// </summary>
    public class SpawnController : MonoBehaviour
    {
        [SerializeField]
        private float maxSpawnVelocity;

        [SerializeField]
        private Trash[] spawnables;

        [SerializeField]
        private float minSpawnTime = 0.25f;

        [SerializeField]
        private float maxSpawnTime = 0.5f;

        [SerializeField]
        private float minX;

        [SerializeField]
        private float maxX;

        protected virtual void Awake()
        {
            // Initial spawn
            Spawn();
        }

        private void Spawn()
        {
            Trash randomTrash = spawnables[Random.Range(0, spawnables.Length)];
            Trash spawnedTrash = Instantiate(randomTrash);

            // Apply some initial velocity
            var trashRigidBody = spawnedTrash.GetComponent<Rigidbody2D>();
            if (trashRigidBody != null)
            {
                Vector2 randomVelocity = Random.insideUnitCircle * maxSpawnVelocity;

                // Always go down
                if (randomVelocity.y > 0)
                {
                    randomVelocity.y = -randomVelocity.y;
                }

                trashRigidBody.velocity = randomVelocity;
            }

            var spawnPosition = new Vector2(Random.Range(minX, maxX), transform.position.y);
            spawnedTrash.transform.position = spawnPosition;

            // Queue next spawn
            Invoke(nameof(Spawn), Random.Range(minSpawnTime, maxSpawnTime));
        }
    }
}
