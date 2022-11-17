using UnityEngine;
using UnityEngine.InputSystem;

// Using embedded actions with callbacks or reading values each frame.

public class ExampleEmbeddedActions : MonoBehaviour
{
    // these embedded actions are configurable in the inspector:
    public InputAction moveAction;
    public InputAction jumpAction;

    public void Awake()
    {
        // assign a callback for the "jump" action.
        jumpAction.performed += ctx => { OnJump(ctx); };
    }

    public void Update()
    {
        // read the value for the "move" action each frame.
        Vector2 moveAmount = moveAction.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // jump code goes here.
    }

    // the actions must be enabled and disabled
    // when the GameObject is enabled or disabled

    public void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    public void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }
}
