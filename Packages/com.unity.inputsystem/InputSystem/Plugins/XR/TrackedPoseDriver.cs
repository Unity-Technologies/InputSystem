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

        /// <summary>
        /// These bit flags correspond with <c>UnityEngine.XR.InputTrackingState</c>
        /// but that enum is not used to avoid adding a dependency to the XR module.
        /// Only the Position and Rotation flags are used by this class, so velocity and acceleration flags are not duplicated here.
        /// </summary>
        [Flags]
        enum TrackingStates
        {
            /// <summary>
            /// Position and rotation are not valid.
            /// </summary>
            None,

            /// <summary>
            /// Position is valid.
            /// See <c>InputTrackingState.Position</c>.
            /// </summary>
            Position = 1 << 0,

            /// <summary>
            /// Rotation is valid.
            /// See <c>InputTrackingState.Rotation</c>.
            /// </summary>
            Rotation = 1 << 1,
        }

        [SerializeField, Tooltip("Which Transform properties to update.")]
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
        /// <seealso cref="InputSystem.onAfterUpdate"/>
        public enum UpdateType
        {
            /// <summary>
            /// Update after the Input System has completed an update and right before rendering.
            /// This is the recommended and default option to minimize lag for XR tracked devices.
            /// </summary>
            /// <seealso cref="InputUpdateType.BeforeRender"/>
            UpdateAndBeforeRender,

            /// <summary>
            /// Update after the Input System has completed an update except right before rendering.
            /// </summary>
            /// <remarks>
            /// This may be dynamic update, fixed update, or a manual update depending on the Update Mode
            /// project setting for Input System.
            /// </remarks>
            Update,

            /// <summary>
            /// Update after the Input System has completed an update right before rendering.
            /// </summary>
            /// <remarks>
            /// Note that this update mode may not trigger if there are no XR devices added which use before render timing.
            /// </remarks>
            /// <seealso cref="InputUpdateType.BeforeRender"/>
            /// <seealso cref="InputDevice.updateBeforeRender"/>
            BeforeRender,
        }

        [SerializeField, Tooltip("Updates the Transform properties after these phases of Input System event processing.")]
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

        [SerializeField, Tooltip("Ignore Tracking State and always treat the input pose as valid.")]
        bool m_IgnoreTrackingState;
        /// <summary>
        /// Ignore tracking state and always treat the input pose as valid when updating the <see cref="Transform"/> properties.
        /// The recommended value is <see langword="false"/> so the tracking state input is used.
        /// </summary>
        /// <seealso cref="trackingStateInput"/>
        public bool ignoreTrackingState
        {
            get => m_IgnoreTrackingState;
            set => m_IgnoreTrackingState = value;
        }

        [SerializeField, Tooltip("The input action to read the position value of a tracked device. Must be a Vector 3 control type.")]
        InputActionProperty m_PositionInput;
        /// <summary>
        /// The input action to read the position value of a tracked device.
        /// Must support reading a value of type <see cref="Vector3"/>.
        /// </summary>
        /// <seealso cref="rotationInput"/>
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

        [SerializeField, Tooltip("The input action to read the rotation value of a tracked device. Must be a Quaternion control type.")]
        InputActionProperty m_RotationInput;
        /// <summary>
        /// The input action to read the rotation value of a tracked device.
        /// Must support reading a value of type <see cref="Quaternion"/>.
        /// </summary>
        /// <seealso cref="positionInput"/>
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

        [SerializeField, Tooltip("The input action to read the tracking state value of a tracked device. Identifies if position and rotation have valid data. Must be an Integer control type.")]
        InputActionProperty m_TrackingStateInput;
        /// <summary>
        /// The input action to read the tracking state value of a tracked device.
        /// Identifies if position and rotation have valid data.
        /// Must support reading a value of type <see cref="int"/>.
        /// </summary>
        /// <remarks>
        /// See [InputTrackingState](xref:UnityEngine.XR.InputTrackingState) enum for values the input action represents.
        /// <list type="bullet">
        /// <item>
        /// <term>[InputTrackingState.None](xref:UnityEngine.XR.InputTrackingState.None) (0)</term>
        /// <description>to indicate neither position nor rotation is valid.</description>
        /// </item>
        /// <item>
        /// <term>[InputTrackingState.Position](xref:UnityEngine.XR.InputTrackingState.Position) (1)</term>
        /// <description>to indicate position is valid.</description>
        /// </item>
        /// <item>
        /// <term>[InputTrackingState.Rotation](xref:UnityEngine.XR.InputTrackingState.Rotation) (2)</term>
        /// <description>to indicate rotation is valid.</description>
        /// </item>
        /// <item>
        /// <term>[InputTrackingState.Position](xref:UnityEngine.XR.InputTrackingState.Position) <c>|</c> [InputTrackingState.Rotation](xref:UnityEngine.XR.InputTrackingState.Rotation) (3)</term>
        /// <description>to indicate position and rotation is valid.</description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <seealso cref="ignoreTrackingState"/>
        public InputActionProperty trackingStateInput
        {
            get => m_TrackingStateInput;
            set
            {
                if (Application.isPlaying)
                    UnbindTrackingState();

                m_TrackingStateInput = value;

                if (Application.isPlaying && isActiveAndEnabled)
                    BindTrackingState();
            }
        }

        Vector3 m_CurrentPosition = Vector3.zero;
        Quaternion m_CurrentRotation = Quaternion.identity;
        TrackingStates m_CurrentTrackingState = TrackingStates.Position | TrackingStates.Rotation;
        bool m_RotationBound;
        bool m_PositionBound;
        bool m_TrackingStateBound;
        bool m_IsFirstUpdate = true;

        void BindActions()
        {
            BindPosition();
            BindRotation();
            BindTrackingState();
        }

        void UnbindActions()
        {
            UnbindPosition();
            UnbindRotation();
            UnbindTrackingState();
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

        void BindTrackingState()
        {
            if (m_TrackingStateBound)
                return;

            var action = m_TrackingStateInput.action;
            if (action == null)
                return;

            action.performed += OnTrackingStatePerformed;
            action.canceled += OnTrackingStateCanceled;
            m_TrackingStateBound = true;

            if (m_TrackingStateInput.reference == null)
            {
                action.Rename($"{gameObject.name} - TPD - Tracking State");
                action.Enable();
            }
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

        void UnbindTrackingState()
        {
            if (!m_TrackingStateBound)
                return;

            var action = m_TrackingStateInput.action;
            if (action == null)
                return;

            if (m_TrackingStateInput.reference == null)
                action.Disable();

            action.performed -= OnTrackingStatePerformed;
            action.canceled -= OnTrackingStateCanceled;
            m_TrackingStateBound = false;
        }

        void OnPositionPerformed(InputAction.CallbackContext context)
        {
            m_CurrentPosition = context.ReadValue<Vector3>();
        }

        void OnPositionCanceled(InputAction.CallbackContext context)
        {
            m_CurrentPosition = Vector3.zero;
        }

        void OnRotationPerformed(InputAction.CallbackContext context)
        {
            m_CurrentRotation = context.ReadValue<Quaternion>();
        }

        void OnRotationCanceled(InputAction.CallbackContext context)
        {
            m_CurrentRotation = Quaternion.identity;
        }

        void OnTrackingStatePerformed(InputAction.CallbackContext context)
        {
            m_CurrentTrackingState = (TrackingStates)context.ReadValue<int>();
        }

        void OnTrackingStateCanceled(InputAction.CallbackContext context)
        {
            m_CurrentTrackingState = TrackingStates.None;
        }

        /// <summary>
        /// This function is called when the user hits the Reset button in the Inspector's context menu
        /// or when adding the component the first time. This function is only called in editor mode.
        /// </summary>
        protected void Reset()
        {
            m_PositionInput = new InputActionProperty(new InputAction("Position", expectedControlType: "Vector3"));
            m_RotationInput = new InputActionProperty(new InputAction("Rotation", expectedControlType: "Quaternion"));
            m_TrackingStateInput = new InputActionProperty(new InputAction("Tracking State", expectedControlType: "Integer"));
        }

        /// <summary>
        /// This function is called when the script instance is being loaded.
        /// </summary>
        protected virtual void Awake()
        {
#if UNITY_INPUT_SYSTEM_ENABLE_VR && ENABLE_VR
            if (HasStereoCamera(out var cameraComponent))
            {
                UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(cameraComponent, true);
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

            // Read current input values when becoming enabled,
            // but wait until after the input update so the input is read at a consistent time
            m_IsFirstUpdate = true;
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
            if (HasStereoCamera(out var cameraComponent))
            {
                UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(cameraComponent, false);
            }
#endif
        }

        /// <summary>
        /// The callback method called after the Input System has completed an update and processed all pending events.
        /// </summary>
        /// <seealso cref="InputSystem.onAfterUpdate"/>
        protected void UpdateCallback()
        {
            if (m_IsFirstUpdate)
            {
                // Update current input values if this is the first update since becoming enabled
                // since the performed callbacks may not have been executed
                if (m_PositionInput.action != null)
                    m_CurrentPosition = m_PositionInput.action.ReadValue<Vector3>();

                if (m_RotationInput.action != null)
                    m_CurrentRotation = m_RotationInput.action.ReadValue<Quaternion>();

                ReadTrackingState();

                m_IsFirstUpdate = false;
            }

            if (InputState.currentUpdateType == InputUpdateType.BeforeRender)
                OnBeforeRender();
            else
                OnUpdate();
        }

        void ReadTrackingState()
        {
            var trackingStateAction = m_TrackingStateInput.action;
            if (trackingStateAction != null && !trackingStateAction.enabled)
            {
                // Treat a disabled action as the default None value for the ReadValue call
                m_CurrentTrackingState = TrackingStates.None;
                return;
            }

            if (trackingStateAction == null || trackingStateAction.m_BindingsCount == 0)
            {
                // Treat an Input Action Reference with no reference the same as
                // an enabled Input Action with no authored bindings, and allow driving the Transform pose.
                m_CurrentTrackingState = TrackingStates.Position | TrackingStates.Rotation;
                return;
            }

            // Grab state.
            var actionMap = trackingStateAction.GetOrCreateActionMap();
            actionMap.ResolveBindingsIfNecessary();
            var state = actionMap.m_State;

            // Get list of resolved controls to determine if a device actually has tracking state.
            var hasResolvedControl = false;
            if (state != null)
            {
                var actionIndex = trackingStateAction.m_ActionIndexInState;
                var totalBindingCount = state.totalBindingCount;
                for (var i = 0; i < totalBindingCount; ++i)
                {
                    unsafe
                    {
                        ref var bindingState = ref state.bindingStates[i];
                        if (bindingState.actionIndex != actionIndex)
                            continue;
                        if (bindingState.isComposite)
                            continue;

                        if (bindingState.controlCount > 0)
                        {
                            hasResolvedControl = true;
                            break;
                        }
                    }
                }
            }

            // Retain the current value if there is no resolved binding.
            // Since the field initializes to allowing position and rotation,
            // this allows for driving the Transform pose always when the device
            // doesn't support reporting the tracking state.
            if (hasResolvedControl)
                m_CurrentTrackingState = (TrackingStates)trackingStateAction.ReadValue<int>();
        }

        /// <summary>
        /// This method is called after the Input System has completed an update and processed all pending events
        /// when the type of update is not <see cref="InputUpdateType.BeforeRender"/>.
        /// </summary>
        protected virtual void OnUpdate()
        {
            if (m_UpdateType == UpdateType.Update ||
                m_UpdateType == UpdateType.UpdateAndBeforeRender)
            {
                PerformUpdate();
            }
        }

        /// <summary>
        /// This method is called after the Input System has completed an update and processed all pending events
        /// when the type of update is <see cref="InputUpdateType.BeforeRender"/>.
        /// </summary>
        protected virtual void OnBeforeRender()
        {
            if (m_UpdateType == UpdateType.BeforeRender ||
                m_UpdateType == UpdateType.UpdateAndBeforeRender)
            {
                PerformUpdate();
            }
        }

        /// <summary>
        /// Updates <see cref="Transform"/> properties with the current input pose values that have been read,
        /// constrained by tracking type and tracking state.
        /// </summary>
        /// <seealso cref="SetLocalTransform"/>
        protected virtual void PerformUpdate()
        {
            SetLocalTransform(m_CurrentPosition, m_CurrentRotation);
        }

        /// <summary>
        /// Updates <see cref="Transform"/> properties, constrained by tracking type and tracking state.
        /// </summary>
        /// <param name="newPosition">The new local position to possibly set.</param>
        /// <param name="newRotation">The new local rotation to possibly set.</param>
        protected virtual void SetLocalTransform(Vector3 newPosition, Quaternion newRotation)
        {
            var positionValid = m_IgnoreTrackingState || (m_CurrentTrackingState & TrackingStates.Position) != 0;
            var rotationValid = m_IgnoreTrackingState || (m_CurrentTrackingState & TrackingStates.Rotation) != 0;

#if HAS_SET_LOCAL_POSITION_AND_ROTATION
            if (m_TrackingType == TrackingType.RotationAndPosition && rotationValid && positionValid)
            {
                transform.SetLocalPositionAndRotation(newPosition, newRotation);
                return;
            }
#endif

            if (rotationValid &&
                (m_TrackingType == TrackingType.RotationAndPosition ||
                 m_TrackingType == TrackingType.RotationOnly))
            {
                transform.localRotation = newRotation;
            }

            if (positionValid &&
                (m_TrackingType == TrackingType.RotationAndPosition ||
                 m_TrackingType == TrackingType.PositionOnly))
            {
                transform.localPosition = newPosition;
            }
        }

        bool HasStereoCamera(out Camera cameraComponent)
        {
            return TryGetComponent(out cameraComponent) && cameraComponent.stereoEnabled;
        }

        #region DEPRECATED

        // Disable warnings that these fields are never assigned to. They are set during Unity deserialization and migrated.
        // ReSharper disable UnassignedField.Local
#pragma warning disable 0649
        [Obsolete]
        [SerializeField, HideInInspector]
        InputAction m_PositionAction;
        /// <summary>
        /// (Deprecated) The action to read the position value of a tracked device.
        /// Must support reading a value of type <see cref="Vector3"/>.
        /// </summary>
        /// <seealso cref="positionInput"/>
        public InputAction positionAction
        {
            get => m_PositionInput.action;
            set => positionInput = new InputActionProperty(value);
        }

        [Obsolete]
        [SerializeField, HideInInspector]
        InputAction m_RotationAction;
        /// <summary>
        /// (Deprecated) The action to read the rotation value of a tracked device.
        /// Must support reading a value of type <see cref="Quaternion"/>.
        /// </summary>
        /// <seealso cref="rotationInput"/>
        public InputAction rotationAction
        {
            get => m_RotationInput.action;
            set => rotationInput = new InputActionProperty(value);
        }
#pragma warning restore 0649
        // ReSharper restore UnassignedField.Local

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#pragma warning disable 0612 // Type or member is obsolete -- Deprecated fields are migrated to new properties.
#pragma warning disable UNT0029 // Pattern matching with null on Unity objects -- Using true null is intentional, not operator== evaluation.
            // We're checking for true null here since we don't want to migrate if the new field is already being used, even if the reference is missing.
            // Migrate the old fields to the new properties added in Input System 1.1.0-pre.6.
            if (m_PositionInput.serializedReference is null && m_PositionInput.serializedAction is null && !(m_PositionAction is null))
                m_PositionInput = new InputActionProperty(m_PositionAction);

            if (m_RotationInput.serializedReference is null && m_RotationInput.serializedAction is null && !(m_RotationAction is null))
                m_RotationInput = new InputActionProperty(m_RotationAction);
#pragma warning restore UNT0029
#pragma warning restore 0612
        }

        #endregion
    }
}
