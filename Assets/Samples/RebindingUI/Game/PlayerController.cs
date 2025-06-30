using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Component that integrates Input System actions with the player object.
    /// </summary>
    [RequireComponent(typeof(Player))]
    [DefaultExecutionOrder(-1)] // We need this to run before Player to avoid potential additional latency
    public class PlayerController : MonoBehaviour
    {
        [Header("Input Action Bindings")]
        [Tooltip("The move action, must generate Vector2")]
        public InputActionReference move;
        [Tooltip("The move action, must generate Vector2")]
        public InputActionReference look;
        [Tooltip("The move action, must generate Button value")]
        public InputActionReference fire;
        [Tooltip("The move action, must generate Button value")]
        public InputActionReference change;

        [Header("Color Output")]
        [Tooltip("The device color output frequency (Hz)")]
        public float colorOutputFrequency = 10.0f;

        [Header("Force Feedback Output")]
        [Tooltip("The device rumble output frequency (Hz)")]
        public float rumbleOutputFrequency = 10.0f;

        // Cached actions to avoid excessive memory allocation on binding callback functions
        private Action<InputAction.CallbackContext> m_OnFire;
        private Action<InputAction.CallbackContext> m_OnChange;

        // Required player reference
        private Player m_Player;

        // Device I/O throttling
        private double m_NextLightUpdateTime;
        private Color m_DeviceColor = Color.black;
        private double m_NextRumbleUpdateTime;
        private float m_DeviceRumble;

        private void Awake()
        {
            // Get required player instance
            m_Player = GetComponent<Player>();
            Debug.Assert(m_Player != null);

            // Create (and cache) actions
            m_OnFire = OnFire;
            m_OnChange = OnChange;

            // Initialize throttling times to allow direct update
            var now = Time.realtimeSinceStartupAsDouble;
            m_NextLightUpdateTime = now;
            m_NextRumbleUpdateTime = now;
        }

        private void OnEnable()
        {
            // Monitor button interaction via callbacks to not miss them
            fire.action.performed += m_OnFire;
            change.action.performed += m_OnChange;

            ApplyRumble(0.0f);
            ApplyLight(Color.black);
        }

        private void OnDisable()
        {
            // Note: When disabling the component we skip throttling to make sure the value reaches the device
            ApplyRumble(0.0f);
            ApplyLight(Color.black);

            fire.action.performed -= m_OnFire;
            change.action.performed -= m_OnChange;
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            // Request that player should be firing.
            m_Player.firing = context.action.IsPressed();
        }

        private void OnChange(InputAction.CallbackContext context)
        {
            // Request player to change mode.
            m_Player.Change();
        }

        private void Update()
        {
            // Sample desired move direction and magnitude based on move input per update.
            m_Player.move = move.action.ReadValue<Vector2>();

            // Sample desired rotation angle based on look input per update:
            // - If the underlying control is a relative control we should not scale with time, but rely
            //   on accumulated provided via action, e.g. accumulated (sum of) deltas since last update.
            // - If the underlying control is absolute, we scale magnitude with elapsed time to sample
            //   the absolute state to behave like a per-update relative delta control.
            if (look != null && look.action != null)
            {
                var timeInvariant = (look.action.activeControl is DeltaControl);
                var scale = timeInvariant ? 1.0f : Time.deltaTime * 300.0f;
                var angle = look.action.ReadValue<Vector2>().x * -1.0f * scale;
                m_Player.Rotate(angle);
            }

            // Use real-time when throttling devices to not be affected by time scale
            var now = Time.realtimeSinceStartupAsDouble;

            // Animate device color, note that we throttle this to avoid output congestion on device side.
            var color = m_Player.GetColor();
            if (now >= m_NextLightUpdateTime && m_DeviceColor != color)
            {
                m_NextLightUpdateTime = NextMultipleOf(now, 1.0f / colorOutputFrequency);
                ApplyLight(color);
            }

            // Animate device rumble, note that we throttle this to avoid output congestion on device side.
            // The else branch makes sure rumble effect is paused if user pauses with motors running.
            var rumble = m_Player.manager.GetShake();
            if (now >= m_NextRumbleUpdateTime && !Mathf.Approximately(m_DeviceRumble, rumble))
            {
                m_NextRumbleUpdateTime = NextMultipleOf(now, 1.0f / rumbleOutputFrequency);
                ApplyRumble(rumble);
            }
        }

        private void ApplyLight(Color color)
        {
            m_DeviceColor = color;

            // There is currently no interface for light effects so we check type
            var gamepad = Gamepad.current;
            var dualShockGamepad = gamepad as DualShockGamepad;
            dualShockGamepad?.SetLightBarColor(color);
        }

        private void ApplyRumble(float value)
        {
            m_DeviceRumble = value;

            // Rumble is currently only supported by gamepads
            var gamepad = Gamepad.current;
            gamepad?.SetMotorSpeeds(value, 0.0f);
        }

        private static double NextMultipleOf(double value, double factor)
        {
            return Math.Round((value / factor), MidpointRounding.AwayFromZero) * factor;
        }
    }
}
