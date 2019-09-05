using InputSamples.Controls;
using UnityEngine;

namespace InputSamples.Controller
{
    /// <summary>
    /// Input manager for controller-style inputs (with analogue sticks and buttons).
    /// </summary>
    public class GamepadInputManager : MonoBehaviour
    {
        // Gamepad input action map.
        private GamepadControls rollingControls;

        /// <summary>
        /// Retrieve current state of analogue stick.
        /// </summary>
        public Vector2 AnalogueValue { get; private set; }

        /// <summary>
        /// Retrieve state of primary button.
        /// </summary>
        public bool PrimaryButtonValue { get; private set; }

        /// <summary>
        /// Retrieve state of secondary button.
        /// </summary>
        public bool SecondaryButtonValue { get; private set; }

        protected virtual void Awake()
        {
            rollingControls = new GamepadControls();

            rollingControls.gameplay.movement.performed += context => AnalogueValue = context.ReadValue<Vector2>();
            rollingControls.gameplay.movement.canceled += context => AnalogueValue = Vector2.zero;

            rollingControls.gameplay.button1Action.performed +=
                context => PrimaryButtonValue = context.ReadValue<float>() > 0.5f;
            rollingControls.gameplay.button1Action.canceled +=
                context => PrimaryButtonValue = false;

            rollingControls.gameplay.button2Action.performed +=
                context => SecondaryButtonValue = context.ReadValue<float>() > 0.5f;
            rollingControls.gameplay.button2Action.canceled +=
                context => SecondaryButtonValue = false;
        }

        protected virtual void OnEnable()
        {
            rollingControls?.Enable();
        }

        protected virtual void OnDisable()
        {
            rollingControls?.Disable();
        }
    }
}
