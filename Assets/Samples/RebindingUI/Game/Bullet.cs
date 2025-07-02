using UnityEngine.Pool;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Represents a projectile with collision detection.
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        [Tooltip("The bullet velocity")]
        public float speed = 1.0f;

        [Tooltip("The bullet movement direction vector")]
        public Vector3 direction = Vector3.forward;

        private IObjectPool<Bullet> m_Pool;
        private GameplayManager m_Manager;
        private bool m_Destroyed;

        public void Initialize(GameplayManager manager, IObjectPool<Bullet> pool)
        {
            m_Manager = manager;
            m_Pool = pool;
        }

        private void Update()
        {
            // Animate bullet
            transform.position += direction * (speed * Time.deltaTime);

            // Destroy bullet if it has exited the game area
            if (!m_Manager.IsInsideGameplayArea(transform.position))
                DestroyBullet();
        }

        void OnEnable()
        {
            m_Destroyed = false;
        }

        private void OnCollisionEnter(Collision other)
        {
            DestroyBullet();
        }

        private void DestroyBullet()
        {
            if (m_Destroyed)
                return;

            // Return this object to the pool
            m_Pool.Release(this);
            m_Destroyed = true;
        }
    }
}
