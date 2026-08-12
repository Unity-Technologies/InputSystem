namespace DocCodeSamples.Tests
{
#region quick-start-guide
using UnityEngine;
using UnityEngine.InputSystem;  // 1. The Input System "using" statement

public class Example : MonoBehaviour
{
    // 2. These variables are to hold the Action references
    InputAction moveAction;
    InputAction jumpAction;

    private void Start()
    {
        // 3. Find the references to the "Move" and "Jump" actions
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    void Update()
    {
        // 4. Read the "Move" action value, which is a 2D vector
        // and the "Jump" action state, which is a boolean value

        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        // your movement code here

        if (jumpAction.IsPressed())
        {
            // your jump code here
        }
    }
}
#endregion
}