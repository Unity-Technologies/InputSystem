using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Component that integrates Input System actions with the player object.
    /// </summary>
    [RequireComponent(typeof(Player))]
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("The move action, must generate Vector2")]
        public InputActionReference move;
        [Tooltip("The move action, must generate Vector2")]
        public InputActionReference look;
        [Tooltip("The move action, must generate Button value")]
        public InputActionReference fire;
        [Tooltip("The move action, must generate Button value")]
        public InputActionReference change;

        [Tooltip("Show player color on deivce when applicable")]
        public bool applyColorToDevice = true;
        [Tooltip("The power multiplier of the rumble effect")]
        public float rumblePower = 0.5f;

        // Cached actions to avoid excessive memory allocation on binding callback functions
        // private Action<InputAction.CallbackContext> m_OnMove;
        // private Action<InputAction.CallbackContext> m_OnLook;
        private Action<InputAction.CallbackContext> m_OnFire;
        private Action<InputAction.CallbackContext> m_OnChange;

        // Cached actions relating to in-game events
        private Action<Color, Color> m_OnColorChange;
        private Action m_OnShakeChanged;

        // Required player reference
        private Player m_Player;

        private void Awake()
        {
            // Get required player instance
            m_Player = GetComponent<Player>();
            Debug.Assert(m_Player != null);

            // Create (and cache) actions
            m_OnFire = OnFire;
            m_OnChange = OnChange;
            m_OnColorChange = OnColorChanged;
            m_OnShakeChanged = OnShakeChanged;
        }

        private void OnEnable()
        {
            // Note that for value based controls we must monitor both performed and canceled.
            // Otherwise we would not reset look and move to zero when controls are no longer actuated.

            // move.action.performed += m_OnMove;
            // move.action.canceled += m_OnMove;
            //
            // look.action.performed += m_OnLook;
            // look.action.canceled += m_OnLook;

            fire.action.performed += m_OnFire;

            change.action.performed += m_OnChange;

            //m_Player.ColorChangedEvent += m_OnColorChange;
            SetDeviceColor(m_Player.GetTargetColor());
            SetDeviceRumble(0.0f);
            //m_Player.manager.ShakeChanged -= m_OnShakeChanged;
        }

        private void OnDisable()
        {
            //m_Player.manager.ShakeChanged -= m_OnShakeChanged;
            SetDeviceRumble(0.0f);

            //m_Player.ColorChangedEvent -= m_OnColorChange;
            SetDeviceColor(Color.black);

            // move.action.performed -= m_OnMove;
            // move.action.canceled -= m_OnMove;
            //
            // look.action.performed -= m_OnLook;
            // look.action.canceled -= m_OnLook;

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
            // Request player to change weapon
            m_Player.ChangeWeapon();
        }

        private void OnColorChanged(Color animatedColor, Color targetColor)
        {
            SetDeviceColor(animatedColor);
        }

        private void OnShakeChanged()
        {
            SetDeviceRumble(m_Player.manager.GetShake());
        }

        private Color m_TargetDeviceColor = Color.black;
        private Color m_DeviceColor = Color.black;

        private float m_DeviceColorOutputFrequency = 5.0f;
        private float m_TimeUntilNextDeviceColor;

        private float m_TargetDeviceRumble;
        private float m_DeviceRumble;

        private float m_DeviceRumbleOutputFrequency = 5.0f;
        private float m_TimeUntilNextDeviceRumble = 0.0f;

        private void SetDeviceColor(Color color)
        {
            m_TargetDeviceColor = color;
        }

        private void SetDeviceRumble(float amount)
        {
            m_TargetDeviceRumble = amount;
        }

        private void Update()
        {
            // Sample desired move direction and magnitude based on move input.
            m_Player.move = move.action.ReadValue<Vector2>();

            // Sample desired rotation angle based on look input:
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

            // Animate device color, note that we throttle this to avoid output congestion on device side.
            m_TargetDeviceColor = m_Player.GetColor();
            if (!Throttle(ref m_TimeUntilNextDeviceColor, m_TargetDeviceColor != m_DeviceColor,
                Time.deltaTime, 1.0f / m_DeviceColorOutputFrequency))
            {
                m_DeviceColor = m_TargetDeviceColor;

                // There is currently no interface for light effects so we check type
                var gamepad = Gamepad.current;
                var dualShockGamepad = gamepad as DualShockGamepad;
                if (dualShockGamepad != null)
                    dualShockGamepad.SetLightBarColor(m_DeviceColor);
            }

            // Animate device rumble, note that we throttle this to avoid output congestion on device side.
            m_TargetDeviceRumble = m_Player.manager.GetShake();
            if (!Throttle(ref m_TimeUntilNextDeviceRumble,
                !Mathf.Approximately(m_TargetDeviceRumble, m_DeviceRumble),
                Time.deltaTime, 1.0f / m_DeviceRumbleOutputFrequency))
            {
                m_DeviceRumble = m_TargetDeviceRumble;

                // Rumble is currently only supported by gamepads
                var gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    gamepad.SetMotorSpeeds(m_DeviceRumble, 0.0f);
                }
            }
        }

        private static bool Throttle(ref float remainingTime, bool condition, float deltaTime, float timeUntilNextEvent)
        {
            remainingTime -= deltaTime;
            if (remainingTime > 0.0f)
                return true; // Enough time has not elapsed
            if (condition)
                remainingTime += timeUntilNextEvent;
            if (remainingTime < 0.0f)
                remainingTime = 0.0f;
            return !condition;
        }
    }
}
