using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example script demonstrating the direct workflow of polling devices in <c>Update</c>.
/// </summary>
public class MyPlayerScript : MonoBehaviour
{
    void Update()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null)
        {
            return; // No gamepad connected.
        }

        if (gamepad.rightTrigger.wasPressedThisFrame)
        {
            // 'Use' code here
        }

        Vector2 move = gamepad.leftStick.ReadValue();
        {
            // 'Move' code here
        }
    }
}
