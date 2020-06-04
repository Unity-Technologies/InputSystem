using System;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// The TrackedPoseDriver component applies the current Pose value of a tracked device to the transform of the GameObject.
    /// TrackedPoseDriver can track multiple types of devices including XR HMDs, controllers, and remotes.
    /// </summary>
    [Serializable]
    [AddComponentMenu("XR/Tracked Pose Driver (New Input System)")]
    public class TrackedPoseDriver : MonoBehaviour
    {
        public enum TrackingType
        {
            RotationAndPosition,
            RotationOnly,
            PositionOnly
        }


        [SerializeField]
        TrackingType m_TrackingType;
        /// <summary>
        /// The tracking type being used by the tracked pose driver
        /// </summary>
        public TrackingType trackingType
        {
            get { return m_TrackingType; }
            set { m_TrackingType = value; }
        }

        public enum UpdateType
        {
            UpdateAndBeforeRender,
            Update,
            BeforeRender,
        }

        [SerializeField]
        UpdateType m_UpdateType = UpdateType.UpdateAndBeforeRender;
        /// <summary>
        /// The update type being used by the tracked pose driver
        /// </summary>
        public UpdateType updateType
        {
            get { return m_UpdateType; }
            set { m_UpdateType = value; }
        }

        [SerializeField]
        InputActionProperty m_PositionAction;
        public InputActionProperty positionAction
        {
            get { return m_PositionAction; }
            set
            {
                bool rebind = false;
                if (m_PositionBound)
                {
                    UnbindPosition();
                    rebind = true;
                }
                m_PositionAction = value;
                if (rebind)
                {
                    BindPosition();
                }
            }
        }

        [SerializeField]
        InputActionProperty m_RotationAction;
        public InputActionProperty rotationAction
        {
            get { return m_RotationAction; }
            set
            {
                bool rebind = false;
                if(m_RotationBound)
                {
                    UnbindRotation();
                    rebind = true;
                }
                m_RotationAction = value;
                if(rebind)
                {
                    BindRotation();
                }
            }
        }

        Vector3 m_CurrentPosition = Vector3.zero;
        Quaternion m_CurrentRotation = Quaternion.identity;
        bool m_RotationBound = false;
        bool m_PositionBound = false;

        public void BindActions()
        {
            BindPosition();
            BindRotation();
        }

        void BindPosition()
        {
            if (!m_PositionBound && m_PositionAction != null)
            {
                m_PositionAction.action?.Rename($"{gameObject.name} - TPD - Position");
                if (m_PositionAction != null && m_PositionAction.action != null)
                {
                    m_PositionAction.action.performed += OnPositionUpdate;
                }
                m_PositionBound = true;
                m_PositionAction.action?.Enable();
            }
        }

        void BindRotation()
        {
            if (!m_RotationBound && m_RotationAction != null)
            {
                m_RotationAction.action?.Rename($"{gameObject.name} - TPD - Rotation");
                if (m_RotationAction != null && m_RotationAction.action != null)
                {
                    m_RotationAction.action.performed += OnRotationUpdate;
                }
                m_RotationBound = true;
                m_RotationAction.action?.Enable();
            }
        }

        public void UnbindActions()
        {
            UnbindPosition();
            UnbindRotation();
        }

        void UnbindPosition()
        {
            if (m_PositionAction != null && m_PositionBound)
            {
                m_PositionAction.action?.Disable();
                if (m_PositionAction != null && m_PositionAction.action != null)
                {
                    m_PositionAction.action.performed -= OnPositionUpdate;
                }
                m_PositionBound = false;
            }
        }

        void UnbindRotation()
        {
            if (m_RotationAction != null && m_RotationBound)
            {
                m_RotationAction.action?.Disable();
                if (m_RotationAction != null && m_RotationAction.action != null)
                {
                    m_RotationAction.action.performed -= OnRotationUpdate;
                }
                m_RotationBound = false;
            }
        }

        void OnPositionUpdate(InputAction.CallbackContext context)
        {
            Debug.Assert(m_PositionBound);
            m_CurrentPosition = context.ReadValue<Vector3>();
        }

        void OnRotationUpdate(InputAction.CallbackContext context)
        {
            Debug.Assert(m_RotationBound);
            m_CurrentRotation = context.ReadValue<Quaternion>();
        }

        protected virtual void Awake()
        {
#if UNITY_INPUT_SYSTEM_ENABLE_VR
            if (HasStereoCamera())
            {
                UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), true);
            }
#endif
        }

        protected void OnEnable()
        {
            InputSystem.onAfterUpdate += UpdateCallback;
            BindActions();
        }

        void OnDisable()
        {
            InputSystem.onAfterUpdate -= UpdateCallback;
            UnbindActions();            
        }

        protected virtual void OnDestroy()
        {
#if UNITY_INPUT_SYSTEM_ENABLE_VR
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

        private bool HasStereoCamera()
        {
            var camera = GetComponent<Camera>();
            return camera != null && camera.stereoEnabled;
        }

        protected virtual void PerformUpdate()
        {
            SetLocalTransform(m_CurrentPosition, m_CurrentRotation);
        }
    }
}
