#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEngine.InputSystem.Samples.ProjectWideActionsSample
{
    public class ProjectWideActionsSample : MonoBehaviour
    {
        [SerializeField] public GameObject cube;

        InputAction move;
        InputAction look;
        InputAction attack;
        InputAction jump;
        InputAction interact;
        InputAction next;
        InputAction previous;
        InputAction sprint;
        InputAction crouch;

        // Start is called before the first frame update
        void Start()
        {
            move = InputSystem.actions.FindAction("Player/Move");
            look = InputSystem.actions.FindAction("Player/Look");
            attack = InputSystem.actions.FindAction("Player/Attack");
            jump = InputSystem.actions.FindAction("Player/Jump");
            interact = InputSystem.actions.FindAction("Player/Interact");
            next = InputSystem.actions.FindAction("Player/Next");
            previous = InputSystem.actions.FindAction("Player/Previous");
            sprint = InputSystem.actions.FindAction("Player/Sprint");
            crouch = InputSystem.actions.FindAction("Player/Crouch");
        }

        // Update is called once per frame
        void Update()
        {
            if (attack.WasPressedThisFrame())
            {
                cube.GetComponent<Renderer>().material.color = Color.red;
            }
            else if (attack.WasReleasedThisFrame())
            {
                cube.GetComponent<Renderer>().material.color = Color.green;
            }

            var moveVal = move.ReadValue<Vector2>();
            if (moveVal.x < 0.0f)
            {
                cube.transform.Translate(new Vector3(-10 * Time.deltaTime, 0, 0));
            }
            else if (moveVal.x > 0.0f)
            {
                cube.transform.Translate(new Vector3(10 * Time.deltaTime, 0, 0));
            }
            if (moveVal.y < 0.0f)
            {
                cube.transform.Translate(new Vector3(0, -10 * Time.deltaTime, 0));
            }
            else if (moveVal.y > 0.0f)
            {
                cube.transform.Translate(new Vector3(0, 10 * Time.deltaTime, 0));
            }
        }
    }
}
#endif
