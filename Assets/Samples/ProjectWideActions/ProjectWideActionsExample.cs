#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

namespace UnityEngine.InputSystem.Samples.ProjectWideActions
{
    public class ProjectWideActionsExample : MonoBehaviour
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
            // Project-Wide Actions
            move = InputSystem.actions.FindAction("Player/Move");
            look = InputSystem.actions.FindAction("Player/Look");
            attack = InputSystem.actions.FindAction("Player/Attack");
            jump = InputSystem.actions.FindAction("Player/Jump");
            interact = InputSystem.actions.FindAction("Player/Interact");
            next = InputSystem.actions.FindAction("Player/Next");
            previous = InputSystem.actions.FindAction("Player/Previous");
            sprint = InputSystem.actions.FindAction("Player/Sprint");
            crouch = InputSystem.actions.FindAction("Player/Crouch");

            // Handle input by responding to callbacks
            attack.performed += ctx => cube.GetComponent<Renderer>().material.color = Color.red;
            attack.canceled += ctx => cube.GetComponent<Renderer>().material.color = Color.green;
        }

        // Update is called once per frame
        void Update()
        {
            // Handle input by polling each frame
            var moveVal = move.ReadValue<Vector2>() * 10.0f * Time.deltaTime;
            cube.transform.Translate(new Vector3(moveVal.x, moveVal.y, 0));
        }
    } // class ProjectWideActionsExample

} // namespace UnityEngine.InputSystem.Samples.ProjectWideActions

#endif
