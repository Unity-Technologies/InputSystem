using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Input.Interactions;

// Use action set asset instead of lose InputActions directly on component.
public class SimpleController_UsingActions_InAsset : MonoBehaviour
{
    public float moveSpeed;
    public float rotateSpeed;
    public float burstSpeed;
    public GameObject projectile;

    public DemoControls controls;

    private Vector2 m_Move;
    private Vector2 m_Look;
    private bool m_Charging;

    private Vector2 m_Rotation;

    public void Awake()
    {
        controls.gameplay.move.performed += ctx => m_Move = ctx.ReadValue<Vector2>();
        controls.gameplay.look.performed += ctx => m_Look = ctx.ReadValue<Vector2>();

        controls.gameplay.fire.performed +=
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
        controls.gameplay.fire.started +=
            ctx =>
        {
            if (ctx.interaction is SlowTapInteraction)
                m_Charging = true;
        };
        controls.gameplay.fire.cancelled +=
            ctx =>
        {
            m_Charging = false;
        };
    }

    public void OnEnable()
    {
        controls.Enable();
    }

    public void OnDisable()
    {
        controls.Disable();
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
        var scaledRoateSpeed = rotateSpeed * Time.deltaTime;
        m_Rotation.y += rotate.x * scaledRoateSpeed;
        m_Rotation.x = Mathf.Clamp(m_Rotation.x - rotate.y * scaledRoateSpeed, -89, 89);
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
