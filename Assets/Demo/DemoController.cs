using System;
using ISX;
using UnityEngine;
using Random = UnityEngine.Random;

public class DemoController : MonoBehaviour
{
    public Transform head;
    public GameObject projectile;
    public float timeBetweenShots = 0.5f;

    [NonSerialized]////FIXME: seems like the recursion prevention code doesn't work properly
    private InputAction m_FireAction;

    public void Awake()
    {
        m_FireAction = new InputAction("Fire", binding: "/*/{primaryAction}");
        m_FireAction.performed += (action, control) => Fire();
    }

    public void Start()
    {
        m_FireAction.Enable();
    }

    void Fire()
    {
        var newProjectile = Instantiate(projectile);
        newProjectile.transform.position = head.position + head.forward * 0.6f;
        newProjectile.transform.rotation = head.rotation;
        var size = 1;
        newProjectile.transform.localScale *= size;
        newProjectile.GetComponent<Rigidbody>().mass = Mathf.Pow(size, 3);
        newProjectile.GetComponent<Rigidbody>().AddForce(head.forward * 20f, ForceMode.Impulse);
        newProjectile.GetComponent<MeshRenderer>().material.color = new Color(Random.value, Random.value, Random.value, 1.0f);
    }
}
