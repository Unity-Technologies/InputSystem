using System;
using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    public float speed = 1.0f;
    public Vector3 direction = Vector3.forward;
    public IObjectPool<Bullet> pool;

    private bool m_Destroyed;

    private void Update()
    {
        // Animate bullet
        transform.position += direction * (speed * Time.deltaTime);

        // Destroy bullet if it has exited the game area
        if (Vector3.Distance(transform.position, Vector3.zero) > 10.0f)
            OnParticleDestroyed();
    }

    void OnEnable()
    {
        m_Destroyed = false;
    }

    private void OnCollisionEnter(Collision other)
    {
        OnParticleDestroyed();
    }

    private void OnParticleDestroyed()
    {
        if (m_Destroyed)
            return;
        pool.Release(this);
        m_Destroyed = true;
    }
}
