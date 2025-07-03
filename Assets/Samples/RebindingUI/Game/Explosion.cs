using UnityEngine;

public class Explosion : MonoBehaviour
{
    public Vector3 explosionPosition;
    private ParticleSystem m_ParticleSystem;
    private Rigidbody[] m_Rigidbodies;
    private bool m_Exploded;
    private bool m_Destroyed;

    private void Awake()
    {
        m_ParticleSystem = GetComponent<ParticleSystem>();
        m_Rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
    }

    private void OnEnable()
    {
        m_ParticleSystem.Play();
        m_Exploded = false;
    }

    private void OnDisable()
    {
        m_ParticleSystem.Stop();
    }

    private void Update()
    {
        if (!m_ParticleSystem.isPlaying && !m_Destroyed)
        {
            m_Destroyed = true;
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (!m_Exploded)
        {
            for (var i = 0; i < m_Rigidbodies.Length; i++)
            {
                var body = m_Rigidbodies[i];
                body.AddExplosionForce(10.0f, explosionPosition, 0.5f, 0.0f, ForceMode.Impulse);
            }

            m_Exploded = true;
        }
    }
}
