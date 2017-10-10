using System;
using ISX;
using UnityEngine;
using Random = UnityEngine.Random;

public class DemoController : MonoBehaviour
{
    public Transform head;
    public GameObject projectile;
    public float timeBetweenShots = 0.5f;
    public float moveSpeed = 5;

    private Vector2 m_Look;
    private Vector2 m_Walk;
    private Vector2 m_Rotation;
    private Rigidbody m_RigidBody;

    ////TODO: put actions in set (actually, load them from actions.json and their bindings from bindings.json)

    [NonSerialized]////FIXME: seems like the recursion prevention code doesn't work properly
    private InputAction m_FireAction;

    [NonSerialized] private InputAction m_LookAction;
    [NonSerialized] private InputAction m_WalkAction;

    public void Awake()
    {
        m_FireAction = new InputAction("Fire", binding: "/*/{primaryAction}");
        m_WalkAction = new InputAction("Walk", binding: "/*/{primaryStick}");
        m_LookAction = new InputAction("Look", binding: "/*/{secondaryStick}");

        ////TODO: ideally should have a way to get values from controls without having to make assumptions about
        ////      what kind of control sits behind the binding
        m_LookAction.performed += (action, control) => m_Look = ((Vector2Control)control).value;
        m_WalkAction.performed += (action, control) => m_Walk = ((Vector2Control)control).value;
        m_FireAction.performed += (action, control) => Fire();
    }

    public void Start()
    {
        m_RigidBody = GetComponent<Rigidbody>();

        m_FireAction.Enable();
        m_LookAction.Enable();
        m_WalkAction.Enable();
    }

    public void Update()
    {
        // Move
        var velocity = transform.TransformDirection(new Vector3(m_Walk.x, 0, m_Walk.y)) * moveSpeed;
        m_RigidBody.velocity = new Vector3(velocity.x, m_RigidBody.velocity.y, velocity.z);

        // Look
        m_Rotation.y += m_Look.x;
        m_Rotation.x = Mathf.Clamp(m_Look.x - m_Look.y, -89, 89);

        transform.localEulerAngles = new Vector3(0, m_Look.y, 0);
        head.localEulerAngles = new Vector3(m_Look.x, 0, 0);
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
