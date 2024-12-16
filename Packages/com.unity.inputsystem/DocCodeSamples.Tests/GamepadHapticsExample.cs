using UnityEngine;
using UnityEngine.InputSystem;

namespace DocCodeSamples.Tests
{
    internal class GamepadHapticsExample : MonoBehaviour
    {
        bool hapticsArePaused = false;

        void Update()
        {
            var gamepad = Gamepad.current;

            // No gamepad connected, no need to continue.
            if (gamepad == null)
                return;

            float leftTrigger = gamepad.leftTrigger.ReadValue();
            float rightTrigger = gamepad.rightTrigger.ReadValue();

            // Only set motor speeds if haptics were not paused and if trigger is actuated.
            // Both triggers must be actuated past 0.2f to start haptics.
            if (!hapticsArePaused &&
                (gamepad.leftTrigger.IsActuated() || gamepad.rightTrigger.IsActuated()))
                gamepad.SetMotorSpeeds(
                    leftTrigger < 0.2f ? 0.0f : leftTrigger,
                    rightTrigger < 0.2f ? 0.0f : rightTrigger);

            // Toggle haptics "playback" when "Button South" is pressed.
            // Notice that if you release the triggers after pausing,
            // and press the button again, haptics will resume.
            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                if (hapticsArePaused)
                    gamepad.ResumeHaptics();
                else
                    gamepad.PauseHaptics();

                hapticsArePaused = !hapticsArePaused;
            }

            // Notice that if you release the triggers after pausing,
            // and press the Start button, haptics will be reset.
            if (gamepad.startButton.wasPressedThisFrame)
                gamepad.ResetHaptics();
        }
    }
}
