using UnityEngine;
using UnityEngine.InputSystem;

namespace DocCodeSamples.Tests
{
    internal class GamepadExample : MonoBehaviour
    {
        void Start()
        {
            // Print all connected gamepads
            Debug.Log(string.Join("\n", Gamepad.all));
        }

        void Update()
        {
            var gamepad = Gamepad.current;

            // No gamepad connected.
            if (gamepad == null)
            {
                return;
            }

            // Check if "Button North" was pressed this frame
            if (gamepad.buttonNorth.wasPressedThisFrame)
            {
                Debug.Log("Button North was pressed");
            }

            // Check if the button control is being continuously actuated and read its value
            if (gamepad.rightTrigger.IsActuated())
            {
                Debug.Log("Right trigger value: " + gamepad.rightTrigger.ReadValue());
            }

            // Read left stick value and perform some code based on the value
            Vector2 move = gamepad.leftStick.ReadValue();
            {
                // Use the Vector2 move for the game logic here
            }

            // Creating haptic feedback while "Button South" is pressed and stopping it when released.
            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                gamepad.SetMotorSpeeds(0.2f, 1.0f);
            }
            else if (gamepad.buttonSouth.wasReleasedThisFrame)
            {
                gamepad.ResetHaptics();
            }
        }
    }
}
