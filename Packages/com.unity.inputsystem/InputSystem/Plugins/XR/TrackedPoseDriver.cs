using System;
using UnityEngine.XR;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
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
        public UpdateType updateType
        {
            get { return m_UpdateType; }
            set { m_UpdateType = value; }
        }

        [SerializeField]
        InputAction m_PositionAction;
        public InputAction positionAction
        {
            get { return m_PositionAction; }
            set
            {
                UnbindPosition();
                m_PositionAction = value;
                BindActions();
            }
        }

        [SerializeField]
        InputAction m_RotationAction;
        public InputAction rotationAction
        {
            get { return m_RotationAction; }
            set
            {
                UnbindRotation();
                m_RotationAction = value;
                BindActions();
            }
        }

        Vector3 m_CurrentPosition = Vector3.zero;
        Quaternion m_CurrentRotation = Quaternion.identity;
        bool m_RotationBound = false;
        bool m_PositionBound = false;

        void BindActions()
        {
            BindPosition();
            BindRotation();
        }

        void BindPosition()
        {
            if (!m_PositionBound && m_PositionAction != null)
            {
                m_PositionAction.performed += OnPositionUpdate;
                m_PositionBound = true;
                m_PositionAction.Enable();
            }
        }

        void BindRotation()
        {
            if (!m_RotationBound && m_RotationAction != null)
            {
                m_RotationAction.performed += OnRotationUpdate;
                m_RotationBound = true;
                m_RotationAction.Enable();
            }
        }

        void UnbindActions()
        {
            UnbindPosition();
            UnbindRotation();
        }

        void UnbindPosition()
        {
            if (m_PositionAction != null && m_PositionBound)
            {
                m_PositionAction.Disable();
                m_PositionAction.performed -= OnPositionUpdate;
                m_PositionBound = false;
            }
        }

        void UnbindRotation()
        {
            if (m_RotationAction != null && m_RotationBound)
            {
                m_RotationAction.Disable();
                m_RotationAction.performed -= OnRotationUpdate;
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
#if ENABLE_VR
            if (HasStereoCamera())
            {
                XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), true);
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
            UnbindActions();
            InputSystem.onAfterUpdate -= UpdateCallback;
        }

        protected virtual void OnDestroy()
        {
#if ENABLE_VR
            if (HasStereoCamera())
            {
                XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), false);
            }
#endif
        }

        protected void UpdateCallback(InputUpdateType type)
        {
            switch (type)
            {
                case InputUpdateType.Dynamic:
                    OnUpdate();
                    break;
                case InputUpdateType.BeforeRender:
                    OnBeforeRender();
                    break;
            }
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
