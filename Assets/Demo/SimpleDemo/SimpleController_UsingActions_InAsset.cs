using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Interactions;

// Use action set asset instead of lose InputActions directly on component.
public class SimpleController_UsingActions_InAsset : MonoBehaviour
{
    public float moveSpeed;
    public float rotateSpeed;
    public float burstSpeed;
    public float jumpForce = 2.0f;
    public GameObject projectile;

    private SimpleControls controls;

    private Vector2 m_Move;
    private Vector2 m_Look;
    private bool isGrounded;
    private bool m_Charging;
    private Vector2 m_Rotation;
    private Rigidbody m_Rigidbody;

    private void Start()
    {		
        m_Rigidbody = GetComponent<Rigidbody>();

        ////FIXME: Solve this properly. ATM, if we have both fixed and dynamic updates enabled, then
        ////       we run into problems as actions will fire in updates while the actual processing of input
        ////       happens in Update(). So, if we're looking at m_Look, for example, we will see mouse deltas
        ////       on it but then also see the deltas get reset between updates meaning that most of the time
        ////       Update() will end up with a zero m_Look vector.
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdateOnly;
    }

    void OnCollisionStay()
    {
        isGrounded = true;
    }

    public void Awake()
    {
		controls = new SimpleControls();
        controls.gameplay.move.performed += ctx => m_Move = ctx.ReadValue<Vector2>();
        controls.gameplay.look.performed += ctx => m_Look = ctx.ReadValue<Vector2>();
        controls.gameplay.move.cancelled += ctx => m_Move = Vector2.zero;
        controls.gameplay.look.cancelled += ctx => m_Look = Vector2.zero;

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
        controls.gameplay.jump.performed += ctx =>
        {
            var jump = new Vector3(0.0f, jumpForce, 0.0f);
            if (isGrounded)
            {
                m_Rigidbody.AddForce(jump * jumpForce, ForceMode.Impulse);
                isGrounded = false;
            }
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
        const float kClampAngle = 80.0f;

        m_Rotation.y += rotate.x * rotateSpeed * Time.deltaTime;
        m_Rotation.x -= rotate.y * rotateSpeed * Time.deltaTime;

        m_Rotation.x = Mathf.Clamp(m_Rotation.x, -kClampAngle, kClampAngle);

        var localRotation = Quaternion.Euler(m_Rotation.x, m_Rotation.y, 0.0f);
        transform.rotation = localRotation;
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
        const int size = 1;
        newProjectile.transform.localScale *= size;
        newProjectile.GetComponent<Rigidbody>().mass = Mathf.Pow(size, 3);
        newProjectile.GetComponent<Rigidbody>().AddForce(transform.forward * 20f, ForceMode.Impulse);
        newProjectile.GetComponent<MeshRenderer>().material.color =
            new Color(Random.value, Random.value, Random.value, 1.0f);
    }
}
