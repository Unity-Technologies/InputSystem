using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.DualShock;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Component that integrates Input System actions with feedback effects.
    /// </summary>
    public class FeedbackController : MonoBehaviour
    {
        private const float kDefaultOutputFrequency = 10.0f;
        private const float kDefaultOutputThrottleDelay = 1.0f / kDefaultOutputFrequency;

        [Header("Color Output")]
        [Tooltip("The device color output frequency (Hz)")]
        public float colorOutputFrequency = kDefaultOutputFrequency;

        [Header("Force Feedback Output")]
        [Tooltip("The device rumble output frequency (Hz)")]
        public float rumbleOutputFrequency = kDefaultOutputFrequency;

        /// <summary>
        /// Gets or sets the target light color.
        /// </summary>
        public Color color { get; set; }

        /// <summary>
        /// Gets or sets the strength of the rumble effect [0, 1].
        /// </summary>
        public float rumble
        {
            get => m_Rumble;
            set => m_Rumble = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Records the device used to trigger an action.
        /// </summary>
        /// <remarks>
        /// This should be called during interaction to assist the feedback controller in understanding
        /// what devices are currently in use to manage feedback across devices.
        /// </remarks>
        /// <param name="action">The associated action that got triggered or cancelled.</param>
        public void RecordRecentDeviceFromAction(InputAction action)
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
        private float m_Rumble;

        private void Awake()
        {
            // Initialize throttling times to allow direct update
            var now = Time.realtimeSinceStartupAsDouble;
            m_NextLightUpdateTime = now;
            m_NextRumbleUpdateTime = now;
        }

        private void OnEnable()
        {
            m_InvalidateLight = true;
            m_InvalidateRumble = true;
        }

        private void OnDisable()
        {
            // "Restore" rumble and light effects of any supported devices.
            // Note: When disabling the component we skip throttling to make sure the value reaches the device.
            ApplyRumble(kNoRumble);
            ApplyLight(NoLight);
        }

        private void Update()
        {
            var now = Time.realtimeSinceStartupAsDouble;

            if (DetectAbandonedDevices(now))
                m_InvalidateLight = m_InvalidateRumble = true;

            // Animate device color, note that we throttle this to avoid output congestion on device side.
            // See https://jira.unity3d.com/browse/ISXB-1587 for why this workaround was added.
            // If this ticket is resolved, frequency settings and this workaround may be removed.
            if (now >= m_NextLightUpdateTime && (m_InvalidateLight || m_DeviceColor != color))
            {
                m_InvalidateLight = false;
                m_NextLightUpdateTime = ComputeNextUpdateTime(now, colorOutputFrequency);
                ApplyLight(color);
            }

            // Animate device rumble, note that we throttle this to avoid output congestion on device side.
            // The else branch makes sure rumble effect is paused if user pauses with motors running.
            // See https://jira.unity3d.com/browse/ISXB-1586 for why this workaround was added.
            // If this ticket is resolved, frequency settings and this workaround may be removed.
            if (now >= m_NextRumbleUpdateTime && (m_InvalidateRumble || !Mathf.Approximately(m_DeviceRumble, rumble)))
            {
                m_InvalidateRumble = false;
                m_NextRumbleUpdateTime = ComputeNextUpdateTime(now, rumbleOutputFrequency);
                ApplyRumble(rumble);
            }
        }

        private void ApplyLight(Color colorValue)
        {
            m_DeviceColor = colorValue;

            // Note: There is currently no interface for light effects so we check type
            // Always allow devices to go back to zero light, but only apply light if device is recently used.
            var now = Time.realtimeSinceStartupAsDouble;
            foreach (var gamepad in Gamepad.all)
            {
                if (IsRecentlyUsed(gamepad, now))
                    ApplyLightToDevice(gamepad, colorValue);
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
                if (IsRecentlyUsed(gamepad, now))
                    ApplyRumbleToDevice(gamepad, value);
                else
                    ApplyRumbleToDevice(gamepad, kNoRumble);
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

        private static bool DetectAbandonedDevices(double realTimeSinceStartup)
        {
            bool removed;
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

        private static double ComputeNextUpdateTime(double now, float frequency)
        {
            var factor = frequency > 0.0 ? 1.0f / frequency : kDefaultOutputThrottleDelay;
            return Math.Ceiling(now / factor) * factor;
        }
    }
}
