using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Interactions;

public class SimpleController_UsingActionQueue : MonoBehaviour
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

    private InputActionTrace m_ActionTrace;

    public void Awake()
    {
        m_ActionTrace = new InputActionTrace();
        controls.gameplay.Get().actionTriggered += m_ActionTrace.RecordAction;
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
        foreach (var eventPtr in m_ActionTrace)
        {
            var phase = eventPtr.phase;
            var action = eventPtr.action;

            if (action == controls.gameplay.fire)
            {
                var interaction = eventPtr.interaction;
                switch (phase)
                {
                    case InputActionPhase.Performed:
                        if (interaction is SlowTapInteraction)
                        {
                            StartCoroutine(BurstFire((int)(eventPtr.duration * burstSpeed)));
                        }
                        else
                        {
                            Fire();
                        }
                        m_Charging = false;
                        break;

                    case InputActionPhase.Started:
                        if (interaction is SlowTapInteraction)
                            m_Charging = true;
                        break;

                    case InputActionPhase.Cancelled:
                        m_Charging = false;
                        break;
                }
            }
            else if (action == controls.gameplay.look)
            {
                m_Look = eventPtr.ReadValue<Vector2>();
            }
            else if (action == controls.gameplay.move)
            {
                m_Move = eventPtr.ReadValue<Vector2>();
            }
        }
        m_ActionTrace.Clear();

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
        const int size = 1;
        newProjectile.transform.localScale *= size;
        newProjectile.GetComponent<Rigidbody>().mass = Mathf.Pow(size, 3);
        newProjectile.GetComponent<Rigidbody>().AddForce(transform.forward * 20f, ForceMode.Impulse);
        newProjectile.GetComponent<MeshRenderer>().material.color =
            new Color(Random.value, Random.value, Random.value, 1.0f);
    }
}
