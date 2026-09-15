using UnityEngine;
using UnityEngine.InputSystem;

// This script is designed to have the OnMove and
// OnJump methods called by a PlayerInput component

/// <summary>
/// Example script demonstrating the <c>PlayerInput</c> "Invoke Unity Events" workflow.
/// </summary>
public class ExampleScript : MonoBehaviour
{
    Vector2 moveAmount;

    /// <summary>
    /// Called by <c>PlayerInput</c> when the "move" action is triggered.
    /// </summary>
    /// <param name="context">Context for the triggered action.</param>
    public void OnMove(InputAction.CallbackContext context)
    {
        // read the value for the "move" action each event call
        moveAmount = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Called by <c>PlayerInput</c> when the "jump" action is triggered.
    /// </summary>
    /// <param name="context">Context for the triggered action.</param>
    public void OnJump(InputAction.CallbackContext context)
    {
        // your jump code goes here.
    }

    /// <summary>
    /// Called once per frame by Unity.
    /// </summary>
    public void Update()
    {
        // to use the Vector2 value from the "move" action each
        // frame, use the "moveAmount" variable here.
    }
}
