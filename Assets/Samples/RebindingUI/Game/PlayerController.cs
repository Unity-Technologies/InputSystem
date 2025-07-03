using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XR;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Component that integrates Input System actions with the player object.
    /// </summary>
    [RequireComponent(typeof(Player))]
    [DefaultExecutionOrder(-1)] // We need this to run before Player to avoid potential additional latency
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

        [Tooltip("Feedback controller handling device feedback")]
        public FeedbackController feedbackController;

        // Cached actions to avoid excessive memory allocation on binding callback functions
        private Action<InputAction.CallbackContext> m_OnFire;
        private Action<InputAction.CallbackContext> m_OnChange;

        // Required player reference
        private Player m_Player;

        private const float kMouseSensitivity = 0.4f;
        private const float kGamepadSensitivity = 1.0f;

        private void Awake()
        {
            // Get required player instance
            m_Player = GetComponent<Player>();
            Debug.Assert(m_Player != null);

            // Create (and cache) actions
            m_OnFire = OnFire;
            m_OnChange = OnChange;
        }

        private void OnEnable()
        {
            // Monitor button interaction via callbacks to not miss them.
            fire.action.performed += m_OnFire;
            change.action.performed += m_OnChange;
        }

        private void OnDisable()
        {
            fire.action.performed -= m_OnFire;
            change.action.performed -= m_OnChange;

            feedbackController.color = Color.black;
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            var isFiring = context.action.IsPressed();

            if (isFiring)
                feedbackController?.RecordRecentDeviceFromAction(context.action);

            m_Player.firing = isFiring;
        }

        private void OnChange(InputAction.CallbackContext context)
        {
            feedbackController?.RecordRecentDeviceFromAction(context.action);

            m_Player.Change();
        }

        private void Update()
        {
            // Sample desired move direction and magnitude based on move input per update.
            var moveValue = move.action.ReadValue<Vector2>();
            m_Player.move = moveValue;
            if (moveValue.sqrMagnitude > 0.05f)
                feedbackController?.RecordRecentDeviceFromAction(move);

            // Sample desired rotation angle based on look input per update:
            // - If the underlying control is a relative control we should not scale with time, but rely
            //   on accumulated provided via action, e.g. accumulated (sum of) deltas since last update.
            // - If the underlying control is absolute, we scale magnitude with elapsed time to sample
            //   the absolute state to behave like a per-update relative delta control.
            if (look != null && look.action != null)
            {
                var lookValue = look.action.ReadValue<Vector2>();
                if (lookValue.sqrMagnitude > 0.05f)
                    feedbackController?.RecordRecentDeviceFromAction(look);

                var timeInvariant = look.action.activeControl is DeltaControl;
                var scale = timeInvariant ?
                    1.0f * kMouseSensitivity :
                    Time.deltaTime * 300.0f * kGamepadSensitivity;
                var angle = lookValue.x * -1.0f * scale;
                m_Player.Rotate(angle);
            }

            // Let player color feedback on device when supported
            if (feedbackController != null)
                feedbackController.color = m_Player.GetColor();
        }
    }
}
