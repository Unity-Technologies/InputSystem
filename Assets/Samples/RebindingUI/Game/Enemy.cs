using UnityEngine.Pool;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A simple enemy for the mini-game.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Tooltip("The rotation animation target")]
        public GameObject animationTarget;

        [Tooltip("The target that the enemy will seek, e.g. player transform")]
        public Transform target;

        [Tooltip("The rotation speed around the X-axis")]
        public float rotationSpeedX = 130.0f;

        [Tooltip("The rotation speed around the Y-axis")]
        public float rotationSpeedY = 100.0f;

        [Tooltip("The movement speed")]
        public float speed = 1.0f;

        [Tooltip("The explosion color")]
        public Color explosionColor = new Color(0.8711135f, 0.5424528f, 1.0f);

        public IObjectPool<Enemy> pool;
        [HideInInspector] public GameplayManager manager;

        private void OnCollisionEnter(Collision other)
        {
            // If we are hit by a bullet apply force
            if (other.gameObject.GetComponent<Bullet>())
            {
                manager.KillEnemy();
                manager.Explosion(animationTarget.transform, other.GetContact(0).point, 0.1f, explosionColor);
                pool.Release(this);
            }
        }

        // Update is called once per frame
        private void Update()
        {
            // Animate rotation
            if (animationTarget)
            {
                animationTarget.transform.Rotate(Vector3.up, rotationSpeedX * Time.deltaTime, Space.World);
                animationTarget.transform.Rotate(Vector3.right, rotationSpeedY * Time.deltaTime, Space.World);
            }

            // Animate movement towards target
            if (target)
                transform.position += (target.position - transform.position).normalized * (Time.deltaTime * speed);

            // Handle enemies getting lost in the world and make them wrap-around
            if (manager.TryTeleportOrthographicExtents(transform.position, out var result))
                transform.position = result;
        }
    }
}
