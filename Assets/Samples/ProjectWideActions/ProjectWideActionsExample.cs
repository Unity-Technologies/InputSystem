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
        InputAction b;
        InputAction shiftB;

        // Start is called before the first frame update
        void Start()
        {
            // Project-Wide Actions
            if (InputSystem.actions)
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
                b = InputSystem.actions.FindAction("Player/B");
                shiftB = InputSystem.actions.FindAction("Player/Shift B");
            }
            else
            {
                Debug.Log("Setup Project Wide Input Actions in the Player Settings, Input System section");
            }

            // Handle input by responding to callbacks
            if (attack != null)
            {
                attack.performed += OnAttack;
                attack.canceled += OnCancel;
            }

            if (b != null)
            {
                b.performed += OnB;
                b.canceled += OnCancel;
            }

            if (shiftB != null)
            {
                shiftB.performed += OnShiftB;
                shiftB.canceled += OnCancel;
            }
        }

        private void OnAttack(InputAction.CallbackContext ctx)
        {
            cube.GetComponent<Renderer>().material.color = Color.red;
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            cube.GetComponent<Renderer>().material.color = Color.green;
        }

        private void OnB(InputAction.CallbackContext ctx)
        {
            Debug.Log("B WAS PRESSED");
            cube.GetComponent<Renderer>().material.color = Color.yellow;
        }

        private void OnShiftB(InputAction.CallbackContext ctx)
        {
            Debug.Log("SHIFT + B WAS PRESSED");
            cube.GetComponent<Renderer>().material.color = Color.blue;
        }

        void OnDestroy()
        {
            if (attack != null)
            {
                attack.performed -= OnAttack;
                attack.canceled -= OnCancel;
            }
            if (b != null)
            {
                b.performed -= OnB;
                b.canceled -= OnCancel;
            }
            if (shiftB != null)
            {
                shiftB.performed -= OnShiftB;
                shiftB.canceled -= OnCancel;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Handle input by polling each frame
            if (move != null)
            {
                var moveVal = move.ReadValue<Vector2>() * 10.0f * Time.deltaTime;
                cube.transform.Translate(new Vector3(moveVal.x, moveVal.y, 0));
            }

            if (shiftB.IsPressed())
            {
                Debug.Log("SHIFT + B WAS PRESSED");
            }

            if (b.IsPressed())
            {
                Debug.Log("B WAS PRESSED");
            }
        }
    } // class ProjectWideActionsExample
} // namespace UnityEngine.InputSystem.Samples.ProjectWideActions
