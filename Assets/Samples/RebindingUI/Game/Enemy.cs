using System;
using UnityEngine;
using UnityEngine.Pool;

public class Enemy : MonoBehaviour
{
    public GameObject animationTarget;
    public Transform target;
    public float rotationSpeedX = 1.0f;
    public float rotationSpeedY = 1.0f;
    public float speed = 1.0f;
    public IObjectPool<Enemy> pool;
    public GameplayManager manager;

    private void OnCollisionEnter(Collision other)
    {
        // If we are hit by a bullet apply force
        if (other.gameObject.GetComponent<Bullet>())
        {
            manager.KillEnemy();
            // TODO Also handle enemies going rouge outside the playing field
            manager.Explosion(animationTarget.transform, other.GetContact(0).point);
            pool.Release(this);
        }
    }

    // Update is called once per frame
    void Update()
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

        if (manager.TryTeleportOrthographicExtents(transform.position, out var result))
            transform.position = result;
    }
}
