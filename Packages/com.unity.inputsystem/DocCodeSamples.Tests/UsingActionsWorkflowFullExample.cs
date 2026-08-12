using UnityEngine;
using UnityEngine.InputSystem;

public class Example : MonoBehaviour
{
    // These variables are to hold the Action references
    InputAction moveAction;
    InputAction jumpAction;

    private void Start()
    {
        // Find the references to the "Move" and "Jump" actions
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    void Update()
    {
        // Read the "Move" action value, which is a 2D vector
        // and the "Jump" action state, which is a boolean value

        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        // your movement code here

        if (jumpAction.IsPressed())
        {
            // your jump code here
        }
    }
}
