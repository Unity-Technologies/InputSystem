using System;
using System.Collections.Generic;
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

        // Track recent controls used for light and haptic force feedback.
        private const double kRecentThresholdSeconds = 3.0;
        private static readonly Dictionary<InputDevice, double> s_RecentlyUsedDevices = new Dictionary<InputDevice, double>();
        private static InputDevice s_MostRecentInputDevice;
        private bool m_InvalidateLight;
        private bool m_InvalidateRumble;

        // Feedback constants
        private static readonly Color NoLight = Color.black;
        private const float kNoRumble = 0.0f;

        // Device I/O throttling
        private double m_NextLightUpdateTime;
        private Color m_DeviceColor = NoLight;
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
            // Monitor button interaction via callbacks to not miss them.
            fire.action.performed += m_OnFire;
            change.action.performed += m_OnChange;

            // Reset rumble and light effects of any supported devices.
            //ApplyRumble(kNoRumble);
            //ApplyLight(Color.black);

            m_InvalidateLight = true;
            m_InvalidateRumble = true;
        }

        private void OnDisable()
        {
            // "Restore" rumble and light effects of any supported devices.
            // Note: When disabling the component we skip throttling to make sure the value reaches the device.
            ApplyRumble(kNoRumble);
            ApplyLight(NoLight);

            fire.action.performed -= m_OnFire;
            change.action.performed -= m_OnChange;
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            var isFiring = context.action.IsPressed();

            if (isFiring)
                RecordRecentDevice(context.action);

            m_Player.firing = isFiring;
        }

        private void OnChange(InputAction.CallbackContext context)
        {
            RecordRecentDevice(context.action);

            m_Player.Change();
        }

        private void Update()
        {
            // Sample desired move direction and magnitude based on move input per update.
            var moveValue = move.action.ReadValue<Vector2>();
            m_Player.move = moveValue;
            if (moveValue.sqrMagnitude > 0.05f)
                RecordRecentDevice(move);

            // Sample desired rotation angle based on look input per update:
            // - If the underlying control is a relative control we should not scale with time, but rely
            //   on accumulated provided via action, e.g. accumulated (sum of) deltas since last update.
            // - If the underlying control is absolute, we scale magnitude with elapsed time to sample
            //   the absolute state to behave like a per-update relative delta control.
            if (look != null && look.action != null)
            {
                var lookValue = look.action.ReadValue<Vector2>();
                if (lookValue.sqrMagnitude > 0.05f)
                    RecordRecentDevice(look);

                var timeInvariant = (look.action.activeControl is DeltaControl);
                var scale = timeInvariant ? 1.0f : Time.deltaTime * 300.0f;
                var angle = lookValue.x * -1.0f * scale;
                m_Player.Rotate(angle);
            }

            // First detect any passive (abandoned devices), e.g. gamepad put down on desk in favor of keyboard/mouse.
            var now = Time.realtimeSinceStartupAsDouble;
            // if (DetectAbandonedDevices(now))
            //     m_InvalidateLight = m_InvalidateRumble = true;

            // Animate device color, note that we throttle this to avoid output congestion on device side.
            var color = m_Player.GetColor();
            if (now >= m_NextLightUpdateTime && (m_InvalidateLight || m_DeviceColor != color))
            {
                m_InvalidateLight = false;
                m_NextLightUpdateTime = NextMultipleOf(now, 1.0f / colorOutputFrequency);
                ApplyLight(color);
            }

            // Animate device rumble, note that we throttle this to avoid output congestion on device side.
            // The else branch makes sure rumble effect is paused if user pauses with motors running.
            var rumble = m_Player.manager.GetShake();
            if (now >= m_NextRumbleUpdateTime && (m_InvalidateRumble || !Mathf.Approximately(m_DeviceRumble, rumble)))
            {
                m_InvalidateRumble = false;
                m_NextRumbleUpdateTime = NextMultipleOf(now, 1.0f / rumbleOutputFrequency);
                ApplyRumble(rumble);
            }
        }

        private void ApplyLight(Color color)
        {
            m_DeviceColor = color;

            // Note: There is currently no interface for light effects so we check type
            // Always allow devices to go back to zero light, but only apply light if device is recently used.
            var now = Time.realtimeSinceStartupAsDouble;
            foreach (var gamepad in Gamepad.all)
            {
                if (/*color == NoLight || */ IsRecentlyUsed(gamepad, now))
                    ApplyLightToDevice(gamepad, color);
                else
                    ApplyLightToDevice(gamepad, NoLight);
            }
        }

        private void ApplyLightToDevice(Gamepad device, Color value)
        {
            var dualShockGamepad = device as DualShockGamepad;
            dualShockGamepad?.SetLightBarColor(value);
        }

        private void ApplyRumble(float value)
        {
            m_DeviceRumble = value;

            // Note: Rumble is currently only supported by gamepads.
            // Always allow devices to go back to zero rumble, but only apply rumble if device is recently used.
            var now = Time.realtimeSinceStartupAsDouble;
            foreach (var gamepad in Gamepad.all)
            {
                if (Mathf.Approximately(value, kNoRumble) || IsRecentlyUsed(gamepad, now))
                    ApplyRumbleToDevice(gamepad, value);
            }
        }

        private void ApplyRumbleToDevice(Gamepad device, float value)
        {
            device.SetMotorSpeeds(value, 0.0f);
        }

        // Note that we track recently used devices to manage feedback effects across devices.
        // Note that this requires appropriate filtering, e.g. dead-zone filtering or relying on non-noisy controls
        // for detection.
        // We do this so that a player using a gamepad will receive feedback effects, but if the player puts
        // down the gamepad and use e.g. keyboard/mouse instead, any feedback on gamepad is undesirable since it
        // may be distracting. Also note that the gamepad might be used even though controls are stationary, e.g.
        // holding fire button but not moving nor looking.

        private static bool IsRecentlyUsed(InputDevice device, double realtimeSinceStartup,
            double thresholdSeconds = kRecentThresholdSeconds)
        {
            return s_MostRecentInputDevice == device || s_RecentlyUsedDevices.ContainsKey(device) &&
                (realtimeSinceStartup - s_RecentlyUsedDevices[device]) < thresholdSeconds;
        }

        private void RecordRecentDevice(InputAction action)
        {
            var control = action.activeControl;
            if (control == null)
                return;

            var device = control.device;
            var now = Time.realtimeSinceStartupAsDouble;

            // If this is a device we haven't seen before or a device coming back to being used we
            // need to make sure we have applied target feedback to it.
            var previouslyRegistered = s_RecentlyUsedDevices.ContainsKey(device);
            if (!previouslyRegistered || (now - s_RecentlyUsedDevices[device]) >= kRecentThresholdSeconds)
            {
                m_InvalidateLight = true;
                m_InvalidateRumble = true;
            }

            // Register device
            s_RecentlyUsedDevices[device] = now;
            s_MostRecentInputDevice = device;
        }

        private static bool DetectAbandonedDevices(double realTimeSinceStartup)
        {
            var removed = false;
            var foundAtLeastOnePassiveDevice = false;
            do
            {
                removed = false;
                foreach (var pair in s_RecentlyUsedDevices)
                {
                    // If device have not been used for a while and its not the most recently used device
                    if (realTimeSinceStartup - pair.Value < kRecentThresholdSeconds ||
                        s_MostRecentInputDevice == pair.Key)
                        continue;

                    // Remove and restart evaluation since invalidated iterators
                    s_RecentlyUsedDevices.Remove(pair.Key);
                    foundAtLeastOnePassiveDevice = true;
                    removed = true;
                    break;
                }
            }
            while (removed);

            return foundAtLeastOnePassiveDevice;
        }

        /*private void ResetFeedbackIfNeeded()
        {

            if (!deviceHasFeedback)
                return;

            // Reset gamepad devices that haven't been used recently
            var realtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
            foreach (var device in m_RecentlyUsedDevices.Keys)
            {
                if (device is Gamepad && (realtimeSinceStartup - m_RecentlyUsedDevices[device]) >= kRecentThresholdSeconds)
                {

                }
            }
        }*/

        private static double NextMultipleOf(double value, double factor)
        {
            return Math.Round((value / factor), MidpointRounding.AwayFromZero) * factor;
        }
    }
}
