using System;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// The <see cref="TrackedPoseDriver"/> component applies the current pose value of a tracked device
    /// to the <see cref="Transform"/> of the <see cref="GameObject"/>.
    /// <see cref="TrackedPoseDriver"/> can track multiple types of devices including XR HMDs, controllers, and remotes.
    /// </summary>
    /// <remarks>
    /// For <see cref="positionInput"/> and <see cref="rotationInput"/>, if an action is directly defined
    /// in the <see cref="InputActionProperty"/>, as opposed to a reference to an action externally defined
    /// in an <see cref="InputActionAsset"/>, the action will automatically be enabled and disabled by this
    /// behavior during <see cref="OnEnable"/> and <see cref="OnDisable"/>. The enabled state for actions
    /// externally defined must be managed externally from this behavior.
    /// </remarks>
    [Serializable]
    [AddComponentMenu("XR/Tracked Pose Driver (Input System)")]
    public class TrackedPoseDriver : MonoBehaviour, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Options for which <see cref="Transform"/> properties to update.
        /// </summary>
        /// <seealso cref="trackingType"/>
        public enum TrackingType
        {
            /// <summary>
            /// Update both rotation and position.
            /// </summary>
            RotationAndPosition,

            /// <summary>
            /// Update rotation only.
            /// </summary>
            RotationOnly,

            /// <summary>
            /// Update position only.
            /// </summary>
            PositionOnly,
        }

        [SerializeField]
        TrackingType m_TrackingType;
        /// <summary>
        /// The tracking type being used by the Tracked Pose Driver
        /// to control which <see cref="Transform"/> properties to update.
        /// </summary>
        /// <seealso cref="TrackingType"/>
        public TrackingType trackingType
        {
            get => m_TrackingType;
            set => m_TrackingType = value;
        }

        /// <summary>
        /// Options for which phases of the player loop will update <see cref="Transform"/> properties.
        /// </summary>
        /// <seealso cref="updateType"/>
        public enum UpdateType
        {
            /// <summary>
            /// Update after the Input System has completed an update and right before rendering.
            /// </summary>
            /// <seealso cref="InputUpdateType.Dynamic"/>
            /// <seealso cref="InputUpdateType.BeforeRender"/>
            UpdateAndBeforeRender,

            /// <summary>
            /// Update after the Input System has completed an update.
            /// </summary>
            /// <seealso cref="InputUpdateType.Dynamic"/>
            Update,

            /// <summary>
            /// Update right before rendering.
            /// </summary>
            /// <seealso cref="InputUpdateType.BeforeRender"/>
            BeforeRender,
        }

        [SerializeField]
        UpdateType m_UpdateType = UpdateType.UpdateAndBeforeRender;
        /// <summary>
        /// The update type being used by the Tracked Pose Driver
        /// to control which phases of the player loop will update <see cref="Transform"/> properties.
        /// </summary>
        /// <seealso cref="UpdateType"/>
        public UpdateType updateType
        {
            get => m_UpdateType;
            set => m_UpdateType = value;
        }

        [SerializeField]
        InputActionProperty m_PositionInput;
        /// <summary>
        /// The action to read the position value of a tracked device.
        /// Must support reading a value of type <see cref="Vector3"/>.
        /// </summary>
        public InputActionProperty positionInput
        {
            get => m_PositionInput;
            set
            {
                if (Application.isPlaying)
                    UnbindPosition();

                m_PositionInput = value;

                if (Application.isPlaying && isActiveAndEnabled)
                    BindPosition();
            }
        }

        [SerializeField]
        InputActionProperty m_RotationInput;
        /// <summary>
        /// The action to read the rotation value of a tracked device.
        /// Must support reading a value of type <see cref="Quaternion"/>.
        /// </summary>
        public InputActionProperty rotationInput
        {
            get => m_RotationInput;
            set
            {
                if (Application.isPlaying)
                    UnbindRotation();

                m_RotationInput = value;

                if (Application.isPlaying && isActiveAndEnabled)
                    BindRotation();
            }
        }

        Vector3 m_CurrentPosition = Vector3.zero;
        Quaternion m_CurrentRotation = Quaternion.identity;
        bool m_RotationBound;
        bool m_PositionBound;

        void BindActions()
        {
            BindPosition();
            BindRotation();
        }

        void BindPosition()
        {
            if (m_PositionBound)
                return;

            var action = m_PositionInput.action;
            if (action == null)
                return;

            action.performed += OnPositionPerformed;
            action.canceled += OnPositionCanceled;
            m_PositionBound = true;

            if (m_PositionInput.reference == null)
            {
                action.Rename($"{gameObject.name} - TPD - Position");
                action.Enable();
            }
        }

        void BindRotation()
        {
            if (m_RotationBound)
                return;

            var action = m_RotationInput.action;
            if (action == null)
                return;

            action.performed += OnRotationPerformed;
            action.canceled += OnRotationCanceled;
            m_RotationBound = true;

            if (m_RotationInput.reference == null)
            {
                action.Rename($"{gameObject.name} - TPD - Rotation");
                action.Enable();
            }
        }

        void UnbindActions()
        {
            UnbindPosition();
            UnbindRotation();
        }

        void UnbindPosition()
        {
            if (!m_PositionBound)
                return;

            var action = m_PositionInput.action;
            if (action == null)
                return;

            if (m_PositionInput.reference == null)
                action.Disable();

            action.performed -= OnPositionPerformed;
            action.canceled -= OnPositionCanceled;
            m_PositionBound = false;
        }

        void UnbindRotation()
        {
            if (!m_RotationBound)
                return;

            var action = m_RotationInput.action;
            if (action == null)
                return;

            if (m_RotationInput.reference == null)
                action.Disable();

            action.performed -= OnRotationPerformed;
            action.canceled -= OnRotationCanceled;
            m_RotationBound = false;
        }

        void OnPositionPerformed(InputAction.CallbackContext context)
        {
            Debug.Assert(m_PositionBound, this);
            m_CurrentPosition = context.ReadValue<Vector3>();
        }

        void OnPositionCanceled(InputAction.CallbackContext context)
        {
            Debug.Assert(m_PositionBound, this);
            m_CurrentPosition = Vector3.zero;
        }

        void OnRotationPerformed(InputAction.CallbackContext context)
        {
            Debug.Assert(m_RotationBound, this);
            m_CurrentRotation = context.ReadValue<Quaternion>();
        }

        void OnRotationCanceled(InputAction.CallbackContext context)
        {
            Debug.Assert(m_RotationBound, this);
            m_CurrentRotation = Quaternion.identity;
        }

        /// <summary>
        /// This function is called when the script instance is being loaded.
        /// </summary>
        protected virtual void Awake()
        {
#if UNITY_INPUT_SYSTEM_ENABLE_VR && ENABLE_VR
            if (HasStereoCamera())
            {
                UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), true);
            }
#endif
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected void OnEnable()
        {
            InputSystem.onAfterUpdate += UpdateCallback;
            BindActions();
        }

        /// <summary>
        /// This function is called when the object becomes disabled or inactive.
        /// </summary>
        protected void OnDisable()
        {
            UnbindActions();
            InputSystem.onAfterUpdate -= UpdateCallback;
        }

        /// <summary>
        /// This function is called when the <see cref="MonoBehaviour"/> will be destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
#if UNITY_INPUT_SYSTEM_ENABLE_VR && ENABLE_VR
            if (HasStereoCamera())
            {
                UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), false);
            }
#endif
        }

        protected void UpdateCallback()
        {
            if (InputState.currentUpdateType == InputUpdateType.BeforeRender)
                OnBeforeRender();
            else
                OnUpdate();
        }

        protected virtual void OnUpdate()
        {
            if (m_UpdateType == UpdateType.Update ||
                m_UpdateType == UpdateType.UpdateAndBeforeRender)
            {
                PerformUpdate();
            }
        }

        protected virtual void OnBeforeRender()
        {
            if (m_UpdateType == UpdateType.BeforeRender ||
                m_UpdateType == UpdateType.UpdateAndBeforeRender)
            {
                PerformUpdate();
            }
        }

        protected virtual void SetLocalTransform(Vector3 newPosition, Quaternion newRotation)
        {
            if (m_TrackingType == TrackingType.RotationAndPosition ||
                m_TrackingType == TrackingType.RotationOnly)
            {
                transform.localRotation = newRotation;
            }

            if (m_TrackingType == TrackingType.RotationAndPosition ||
                m_TrackingType == TrackingType.PositionOnly)
            {
                transform.localPosition = newPosition;
            }
        }

        bool HasStereoCamera()
        {
            var cameraComponent = GetComponent<Camera>();
            return cameraComponent != null && cameraComponent.stereoEnabled;
        }

        protected virtual void PerformUpdate()
        {
            SetLocalTransform(m_CurrentPosition, m_CurrentRotation);
        }

        #region DEPRECATED

        // Disable warnings that these fields are never assigned to. They are set during Unity deserialization and migrated.
        // ReSharper disable UnassignedField.Local
#pragma warning disable 0649
        [Obsolete]
        [SerializeField, HideInInspector]
        InputAction m_PositionAction;
        public InputAction positionAction
        {
            get => m_PositionInput.action;
            set => positionInput = new InputActionProperty(value);
        }

        [Obsolete]
        [SerializeField, HideInInspector]
        InputAction m_RotationAction;
        public InputAction rotationAction
        {
            get => m_RotationInput.action;
            set => rotationInput = new InputActionProperty(value);
        }
#pragma warning restore 0649
        // ReSharper restore UnassignedField.Local

        /// <summary>
        /// Stores whether the fields of type <see cref="InputAction"/> have been migrated to fields of type <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField, HideInInspector]
        bool m_HasMigratedActions;

        /// <summary>
        /// This function is called when the user hits the Reset button in the Inspector's context menu
        /// or when adding the component the first time. This function is only called in editor mode.
        /// </summary>
        protected void Reset()
        {
            m_HasMigratedActions = true;
        }

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_HasMigratedActions)
                return;

#pragma warning disable 0612
            m_PositionInput = new InputActionProperty(m_PositionAction);
            m_RotationInput = new InputActionProperty(m_RotationAction);
            m_HasMigratedActions = true;
#pragma warning restore 0612
        }

        #endregion
    }
}
