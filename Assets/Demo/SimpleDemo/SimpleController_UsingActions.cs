using System.Collections;
using UnityEngine.Experimental.Input;
using UnityEngine;
using UnityEngine.Experimental.Input.Interactions;

// Using simple actions with callbacks.
public class SimpleController_UsingActions : MonoBehaviour
{
    public float moveSpeed;
    public float rotateSpeed;
    public float burstSpeed;
    public GameObject projectile;

    public InputAction moveAction;
    public InputAction lookAction;
    public InputAction fireAction;

    private Vector2 m_Move;
    private Vector2 m_Look;
    private bool m_Charging;

    private Vector2 m_Rotation;

    public void Awake()
    {
        moveAction.performed += ctx => m_Move = ctx.ReadValue<Vector2>();
        lookAction.performed += ctx => m_Look = ctx.ReadValue<Vector2>();
        moveAction.cancelled += ctx => m_Move = Vector2.zero;
        lookAction.cancelled += ctx => m_Look = Vector2.zero;

        fireAction.performed +=
            ctx =>
        {
            if (ctx.interaction is SlowTapInteraction)
            {
                StartCoroutine(BurstFire((int)(ctx.duration * burstSpeed)));
            }
            else
            {
                Fire();
            }
            m_Charging = false;
        };
        fireAction.started +=
            ctx =>
        {
            if (ctx.interaction is SlowTapInteraction)
                m_Charging = true;
        };
        fireAction.cancelled +=
            ctx =>
        {
            m_Charging = false;
        };
    }

    public void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        fireAction.Enable();
    }

    public void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        fireAction.Disable();
    }

    public void OnGUI()
    {
        if (m_Charging)
            GUI.Label(new Rect(100, 100, 200, 100), "Charging...");
    }

    public void Update()
    {
        Move(m_Move);
        Look(m_Look);
    }

    private void Move(Vector2 direction)
    {
        var scaledMoveSpeed = moveSpeed * Time.deltaTime;
        var move = transform.TransformDirection(direction.x, 0, direction.y);
        transform.localPosition += move * scaledMoveSpeed;
    }

    private void Look(Vector2 rotate)
    {
        var scaledRotateSpeed = rotateSpeed * Time.deltaTime;
        m_Rotation.y += rotate.x * scaledRotateSpeed;
        m_Rotation.x = Mathf.Clamp(m_Rotation.x - rotate.y * scaledRotateSpeed, -89, 89);
        transform.localEulerAngles = m_Rotation;
    }

    private IEnumerator BurstFire(int burstAmount)
    {
        for (var i = 0; i < burstAmount; ++i)
        {
            Fire();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Fire()
    {
        var transform = this.transform;
        var newProjectile = Instantiate(projectile);
        newProjectile.transform.position = transform.position + transform.forward * 0.6f;
        newProjectile.transform.rotation = transform.rotation;
        var size = 1;
        newProjectile.transform.localScale *= size;
        newProjectile.GetComponent<Rigidbody>().mass = Mathf.Pow(size, 3);
        newProjectile.GetComponent<Rigidbody>().AddForce(transform.forward * 20f, ForceMode.Impulse);
        newProjectile.GetComponent<MeshRenderer>().material.color =
            new Color(Random.value, Random.value, Random.value, 1.0f);
    }
}
