using ISX;
using UnityEngine;
using Random = UnityEngine.Random;

public class DemoController : MonoBehaviour
{
    public Transform head;
    public GameObject projectile;
    public float timeBetweenShots = 0.5f;
    public float moveSpeed = 5;

    public InputAction fireAction;
    public InputAction lookAction;
    public InputAction walkAction;

    private Vector2 m_Look;
    private Vector2 m_Walk;
    private Vector2 m_Rotation;
    private Rigidbody m_RigidBody;

    ////TODO: put actions in set (actually, load them from actions.json and their bindings from bindings.json)

    public void Awake()
    {
        ////TODO: ideally should have a way to get values from controls without having to make assumptions about
        ////      what kind of control sits behind the binding

        lookAction.performed += (action, control) => m_Look = ((Vector2Control)control).value;
        walkAction.performed += (action, control) => m_Walk = ((Vector2Control)control).value;
        fireAction.performed += (action, control) => Fire();
    }

    public void Start()
    {
        m_RigidBody = GetComponent<Rigidbody>();

        fireAction.Enable();
        lookAction.Enable();
        walkAction.Enable();
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
