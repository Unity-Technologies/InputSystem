using System.Text;
using Cinemachine;
using InputSamples.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace InputSamples.Demo.Rolling
{
    /// <summary>
    /// Controller for player ball.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Ball : MonoBehaviour
    {
        [SerializeField]
        private GamepadInputManager inputManager;
        [SerializeField]
        private Transform cameraTransform;
        [SerializeField]
        private CinemachineVirtualCamera cinemachineTrackingCamera;

        [SerializeField]
        private float respawnY = -5.0f;

        [Header("Forces")]
        [SerializeField]
        private float ballAcceleration = 5.0f;
        [SerializeField]
        private float ballAccelerationInAir = 1.0f;
        [SerializeField]
        private float jumpAcceleration = 30.0f;
        [SerializeField]
        private float airSpinAcceleration = 8.0f;
        [SerializeField]
        private float jumpHoldTime = 0.5f;
        [SerializeField]
        private float brakeAcceleration = 7.5f;
        [SerializeField]
        private float upDotThreshold = 0.8f;

        [Header("Drag Settings")]
        [SerializeField]
        private float airDrag = 0.02f;

        [SerializeField]
        private float groundDragMoving = 0.1f;

        [SerializeField]
        private Text debugLabel;

        private Rigidbody cachedRigidbody;
        private bool grounded;

        private bool jumpHeld;
        private float jumpTimer;
        private float lastTouchedFriction;

        protected virtual void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody>();
            lastTouchedFriction = 0.0f;
        }

        protected virtual void FixedUpdate()
        {
            if (inputManager == null || cameraTransform == null)
            {
                return;
            }

            Vector2 inputState = inputManager.AnalogueValue;
            bool moving = inputState.sqrMagnitude > 0;

            if (moving)
            {
                // This comes in normalized from the input manager, but we clamp it here just to make sure, and to
                // make sure input configuration issues can't make the ball move faster than it should
                inputState = Vector2.ClampMagnitude(inputState, 1.0f);

                // Forward vector relative to camera, y-up
                Vector3 flatCameraForward = cameraTransform.forward;
                flatCameraForward.y = 0;
                flatCameraForward.Normalize();

                // Side vector relative to camera, y-up
                Vector3 flatCameraRight = cameraTransform.right;
                flatCameraForward.y = 0;
                flatCameraForward.Normalize();

                // Calculate input state relative to camera
                Vector3 ballWorldVector = (flatCameraForward * inputState.y) +
                    (flatCameraRight * inputState.x);

                Vector3 ballAccelerationVector = ballWorldVector *
                    (grounded ? ballAcceleration : ballAccelerationInAir);

                cachedRigidbody.AddForce(ballAccelerationVector, ForceMode.Acceleration);

                // If we're in the air, spin the ball
                if (!grounded)
                {
                    Vector3 rotateVector = Vector3.Cross(Vector3.up, ballWorldVector);
                    cachedRigidbody.AddTorque(rotateVector * airSpinAcceleration, ForceMode.Acceleration);
                }
            }

            // Jump height proportional to how long you're holding the button
            var shouldJump = false;
            if (inputManager.PrimaryButtonValue)
            {
                shouldJump = jumpTimer > 0.0f;
            }
            else
            {
                // If we're not holding our jump button, our jump timer either:
                //  - resets if we're grounded (so you can jump on the next press)
                //  - clears if we're in the air (so you can't jump again until you land)
                jumpTimer = grounded ? jumpHoldTime : 0.0f;
            }

            if (shouldJump)
            {
                cachedRigidbody.AddForce(Vector3.up * jumpAcceleration, ForceMode.Acceleration);

                jumpTimer -= Time.fixedDeltaTime;
            }

            // Super brake
            if (inputManager.SecondaryButtonValue)
            {
                Vector3 brakeForce = -cachedRigidbody.velocity.normalized * brakeAcceleration;
                cachedRigidbody.AddForce(brakeForce, ForceMode.Acceleration);
            }

            // Set drag value based on whether or not we're in the air, and whether there's any input
            if (moving)
            {
                cachedRigidbody.drag = grounded ? groundDragMoving : airDrag;
            }
            else
            {
                cachedRigidbody.drag = grounded ? lastTouchedFriction : airDrag;
            }

            // Respawn behaviour
            if (cachedRigidbody.position.y < respawnY)
            {
                Respawn();
            }

            // Reset grounded value
            if (!cachedRigidbody.IsSleeping())
            {
                grounded = false;
            }

            if (debugLabel != null)
            {
                var builder = new StringBuilder();
                builder.AppendFormat("Velocity: {0}, m: {1}", cachedRigidbody.velocity,
                    cachedRigidbody.velocity.magnitude);
                builder.AppendLine();

                builder.AppendFormat("Grounded: {0}", grounded);

                debugLabel.text = builder.ToString();
            }
        }

        protected virtual void OnCollisionEnter(Collision other)
        {
            CheckCollisionForGrounded(other);
        }

        private void OnCollisionStay(Collision other)
        {
            CheckCollisionForGrounded(other);
        }

        private bool CheckCollisionForGrounded(Collision collision)
        {
            if (collision.contacts.Length > 0)
            {
                // Calculate average contact vector
                Vector3 normalSum = Vector3.zero;
                foreach (ContactPoint otherContact in collision.contacts)
                {
                    normalSum += otherContact.normal;
                }

                Vector3 averageNormalized = (normalSum / collision.contacts.Length).normalized;

                // Check how much this surface is pointing in the up direction, to decide whether we're grounded
                if (Vector3.Dot(averageNormalized, Vector3.up) >= upDotThreshold)
                {
                    grounded = true;
                    lastTouchedFriction = collision.collider.sharedMaterial.dynamicFriction;
                    return true;
                }
            }

            return false;
        }

        private void Respawn()
        {
            Vector3 positionChange = Checkpoint.LastCheckpoint - cachedRigidbody.position;

            cachedRigidbody.position = Checkpoint.LastCheckpoint;
            cachedRigidbody.velocity = Vector3.zero;

            if (cinemachineTrackingCamera != null)
            {
                cinemachineTrackingCamera.OnTargetObjectWarped(transform, positionChange);
            }
        }
    }
}
