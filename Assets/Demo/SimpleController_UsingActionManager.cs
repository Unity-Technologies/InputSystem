using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Interactions;

////TODO: transform previous form that went to 'onEvent' to consuming events in bulk

public class SimpleController : MonoBehaviour
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

    private InputActionManager m_ActionManager;

    public void Awake()
    {
        m_ActionManager = new InputActionManager();

        ////TODO: this currently falls over due to missing support for composites in InputActionManager
        ////TEMP: we don't yet have support for setting up composite bindings in the UI; hack
        ////      in WASD keybindings as a temp workaround
        controls.gameplay.move.AppendCompositeBinding("Dpad")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s");

        m_ActionManager.AddActionMap(controls.gameplay);
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
        var triggerEvents = m_ActionManager.triggerEventsForCurrentFrame;
        var triggerEventCount = triggerEvents.Count;

        for (var i = 0; i < triggerEventCount; ++i)
        {
            var actions = triggerEvents[i].actions;
            var actionCount = actions.Count;

            ////REVIEW: this is an insanely awkward way of associating actions with responses
            ////        the API needs serious work

            for (var n = 0; n < actionCount; ++n)
            {
                var action = actions[n].action;
                var phase = actions[n].phase;

                if (action == controls.gameplay.fire)
                {
                    var interaction = actions[n].interaction;
                    switch (phase)
                    {
                        case InputActionPhase.Performed:
                            if (interaction is SlowTapInteraction)
                            {
                                //need start time
                                //StartCoroutine(BurstFire((int) (ctx.duration * burstSpeed)));
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
                    m_Look = triggerEvents[i].ReadValue<Vector2>();
                }
                else if (action == controls.gameplay.move)
                {
                    m_Move = triggerEvents[i].ReadValue<Vector2>();
                }
            }
        }

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
