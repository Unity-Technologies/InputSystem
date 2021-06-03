// This example demonstrates how to display text in the UI that involves action bindings.
// When the player switches control schemes or customizes controls (the latter is not set up
// in this example but if supported, would work with the existing code as is), text that
// is shown to the user may be affected.
//
// In the example, the player is able to move around the world and look at objects (simple
// cubes). When an object is in sight, the player can pick the object with a button. While
// having an object picked up, the player can then either throw the object or drop it back
// on the ground.
//
// Depending on the current context, we display hints in the UI that reflect the currently
// active bindings.

using UnityEngine.UI;

namespace UnityEngine.InputSystem.Samples.InGameHints
{
    public class InGameHintsExample : MonoBehaviour
    {
        public Text helpText;
        public float moveSpeed;
        public float rotateSpeed;
        public float throwForce;
        public float pickupDistance;
        public float holdDistance;

        private Vector2 m_Rotation;

        private enum State
        {
            Wandering,
            ObjectInSights,
            ObjectPickedUp
        }

        private PlayerInput m_PlayerInput;
        private State m_CurrentState;
        private Transform m_CurrentObject;
        private MaterialPropertyBlock m_PropertyBlock;

        // Cached help texts so that we don't generate garbage all the time. Could even cache them by control
        // scheme to not create garbage during control scheme switching but we consider control scheme switches
        // rare so not worth the extra cost in complexity and memory.
        private string m_LookAtObjectHelpText;
        private string m_ThrowObjectHelpText;

        private const string kDefaultHelpTextFormat = "Move close to one of the cubes and look at it to pick up";
        private const string kLookAtObjectHelpTextFormat = "Press {pickup} to pick object up";
        private const string kThrowObjectHelpTextFormat = "Press {throw} to throw object; press {drop} to drop object";

        public void Awake()
        {
            m_PlayerInput = GetComponent<PlayerInput>();
        }

        public void OnEnable()
        {
            ChangeState(State.Wandering);
        }

        // This is invoked by PlayerInput when the controls on the player change. If the player switches control
        // schemes or keyboard layouts, we end up here and re-generate our hints.
        public void OnControlsChanged()
        {
            UpdateUIHints(regenerate: true); // Force re-generation of our cached text strings to pick up new bindings.
        }

        private int m_UpdateCount;

        public void Update()
        {
            var move = m_PlayerInput.actions["move"].ReadValue<Vector2>();
            var look = m_PlayerInput.actions["look"].ReadValue<Vector2>();

            Move(move);
            Look(look);

            switch (m_CurrentState)
            {
                case State.Wandering:
                case State.ObjectInSights:
                    // While looking around for an object to pick up, we constantly raycast into the world.
                    if (Physics.Raycast(transform.position, transform.forward, out var hitInfo,
                        pickupDistance) && !hitInfo.collider.gameObject.isStatic)
                    {
                        if (m_CurrentState != State.ObjectInSights)
                            ChangeState(State.ObjectInSights);
                        m_CurrentObject = hitInfo.transform;

                        // Set a custom color override on the object by installing our property block.
                        if (m_PropertyBlock == null)
                        {
                            m_PropertyBlock = new MaterialPropertyBlock();
                            m_PropertyBlock.SetColor("_Color", new Color(0.75f, 0, 0));
                        }
                        m_CurrentObject.GetComponent<MeshRenderer>().SetPropertyBlock(m_PropertyBlock);
                    }
                    else if (m_CurrentState != State.Wandering)
                    {
                        // No longer have object in sight.
                        ChangeState(State.Wandering);

                        if (m_CurrentObject != null)
                        {
                            // Clear property block on renderer to get rid of our custom color override.
                            m_CurrentObject.GetComponent<Renderer>().SetPropertyBlock(null);
                            m_CurrentObject = null;
                        }
                    }

                    if (m_PlayerInput.actions["pickup"].triggered && m_CurrentObject != null)
                    {
                        PickUp();
                        ChangeState(State.ObjectPickedUp);
                    }
                    break;

                case State.ObjectPickedUp:
                    // If the player hits the throw button, throw the currently carried object.
                    // For this example, let's call this good enough. In a real game, we'd want to avoid the raycast
                    if (m_PlayerInput.actions["throw"].triggered)
                    {
                        Throw();
                        ChangeState(State.Wandering);
                    }
                    else if (m_PlayerInput.actions["drop"].triggered)
                    {
                        Throw(drop: true);
                        ChangeState(State.Wandering);
                    }
                    break;
            }
        }

        private void ChangeState(State newState)
        {
            switch (newState)
            {
                case State.Wandering:
                    break;
                case State.ObjectInSights:
                    break;
                case State.ObjectPickedUp:
                    break;
            }

            m_CurrentState = newState;
            UpdateUIHints();
        }

        private void UpdateUIHints(bool regenerate = false)
        {
            if (regenerate)
            {
                m_ThrowObjectHelpText = default;
                m_LookAtObjectHelpText = default;
            }

            switch (m_CurrentState)
            {
                case State.ObjectInSights:
                    if (m_LookAtObjectHelpText == null)
                        m_LookAtObjectHelpText = kLookAtObjectHelpTextFormat.Replace("{pickup}",
                            m_PlayerInput.actions["pickup"].GetBindingDisplayString());
                    helpText.text = m_LookAtObjectHelpText;
                    break;

                case State.ObjectPickedUp:
                    if (m_ThrowObjectHelpText == null)
                        m_ThrowObjectHelpText = kThrowObjectHelpTextFormat
                            .Replace("{throw}", m_PlayerInput.actions["throw"].GetBindingDisplayString())
                            .Replace("{drop}", m_PlayerInput.actions["drop"].GetBindingDisplayString());
                    helpText.text = m_ThrowObjectHelpText;
                    break;

                default:
                    helpText.text = kDefaultHelpTextFormat;
                    break;
            }
        }

        // Throw or drop currently picked up object.
        private void Throw(bool drop = false)
        {
            // Unmount it.
            m_CurrentObject.parent = null;

            // Turn physics back on.
            var rigidBody = m_CurrentObject.GetComponent<Rigidbody>();
            rigidBody.isKinematic = false;

            // Apply force.
            if (!drop)
                rigidBody.AddForce(transform.forward * throwForce, ForceMode.Impulse);

            m_CurrentObject = null;
        }

        private void PickUp()
        {
            // Mount to our transform.
            m_CurrentObject.position = default;
            m_CurrentObject.SetParent(transform, worldPositionStays: false);
            m_CurrentObject.localPosition += new Vector3(0, 0, holdDistance);

            // Remove color override.
            m_CurrentObject.GetComponent<Renderer>().SetPropertyBlock(null);

            // We don't want the object to be governed by physics while we hold it so turn it into a
            // kinematics body.
            m_CurrentObject.GetComponent<Rigidbody>().isKinematic = true;
        }

        private void Move(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01)
                return;
            var scaledMoveSpeed = moveSpeed * Time.deltaTime;
            // For simplicity's sake, we just keep movement in a single plane here. Rotate
            // direction according to world Y rotation of player.
            var move = Quaternion.Euler(0, transform.eulerAngles.y, 0) * new Vector3(direction.x, 0, direction.y);
            transform.position += move * scaledMoveSpeed;
        }

        private void Look(Vector2 rotate)
        {
            if (rotate.sqrMagnitude < 0.01)
                return;
            var scaledRotateSpeed = rotateSpeed * Time.deltaTime;
            m_Rotation.y += rotate.x * scaledRotateSpeed;
            m_Rotation.x = Mathf.Clamp(m_Rotation.x - rotate.y * scaledRotateSpeed, -89, 89);
            transform.localEulerAngles = m_Rotation;
        }
    }
}
