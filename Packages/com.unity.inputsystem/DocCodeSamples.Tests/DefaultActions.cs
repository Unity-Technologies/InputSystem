using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example script demonstrating how to use the generated default actions class.
/// </summary>
public class DefaultActions : MonoBehaviour
{
    #region default-actions
    void Start()
    {
        // Create an instance of the default actions.
        var actions = new DefaultInputActions();
        actions.Player.Look.performed += OnLook;
        actions.Player.Move.performed += OnMove;
        actions.Enable();
    }

    #endregion

    void OnLook(InputAction.CallbackContext context)
    {
        // your look code here
    }

    void OnMove(InputAction.CallbackContext context)
    {
        // your move code here
    }
}
