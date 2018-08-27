using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.XR.Interaction;

namespace UnityEngine.XR.Experimental
{

    [DefaultExecutionOrder(-30000)]
    [Serializable]
    [AddComponentMenu("XR/Tracked Pose Driver (New Input System)")]
    public class TrackedPoseDriverNIS : MonoBehaviour
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
        public InputAction positionAction {  get { return m_PositionAction;  } set { m_PositionAction = value; m_PositionBound = false;  BindActions(); } }

        [SerializeField]
        InputAction m_RotationAction;
        public InputAction rotationAction { get { return m_RotationAction; } set { m_RotationAction = value; m_RotationBound = false; BindActions();  } }
      
        [SerializeField]
        BasePoseProvider m_PoseProviderComponent = null;
        public BasePoseProvider poseProviderComponent
        {
            get { return m_PoseProviderComponent; }
            set
            {
                m_PoseProviderComponent = value;
                if (value != null)
                {
                    UnbindActions();
                }
                else
                {
                    BindActions();
                }
            }
        }

        Vector3 m_CurrentPosition = Vector3.zero;
        Quaternion m_CurrentRotation = Quaternion.identity;
        bool m_RotationBound = false;
        bool m_PositionBound = false;

        void BindActions()
        {
            if (m_PoseProviderComponent == null)
            {
                if (!m_PositionBound && m_PositionAction != null)
                {
                    m_PositionAction.performed += OnPositionUpdate;
                    m_PositionBound = true;
                }
                if (!m_RotationBound && m_RotationAction != null)
                {
                    m_RotationAction.performed += OnRotationUpdate;
                    m_RotationBound = true;
                }
            }
        }

        void UnbindActions()
        {
            if (m_PositionAction != null && m_PositionBound)
            {
                m_PositionAction.performed -= OnPositionUpdate;
                m_PositionBound = false;
            }
            if (m_RotationAction != null && m_RotationBound)
            {
                m_RotationAction.performed -= OnRotationUpdate;
                m_RotationBound = false;
            }
        }

        void OnPositionUpdate(InputAction.CallbackContext context)
        {
            if (m_PositionBound)
            {
                var vec3Control = context.control as Vector3Control;
                if (vec3Control != null)
                {
                    m_CurrentPosition = vec3Control.ReadValue();
                }
            }
        }

        void OnRotationUpdate(InputAction.CallbackContext context)
        {
            if (m_RotationBound)
            {
                var quatControl = context.control as QuaternionControl;
                if (quatControl != null)
                {
                    m_CurrentRotation = quatControl.ReadValue();
                }
            }
        }

        protected virtual void Awake()
        {           
            if (HasStereoCamera())
            {
                XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), true);
            }
        }

        protected void OnEnable()
        {
            BindActions();
        }

        void OnDisable()
        {
            UnbindActions();
        }

        protected virtual void OnDestroy()
        {
            if (HasStereoCamera())
            {
#if ENABLE_VR
                XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), false);
#endif
            }
        }

        protected virtual void FixedUpdate()
        {
            if (m_UpdateType == UpdateType.Update ||
                m_UpdateType == UpdateType.UpdateAndBeforeRender)
            {
                PerformUpdate();
            }
        }

        protected virtual void Update()
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
            Camera camera = GetComponent<Camera>();
            return camera != null && camera.stereoEnabled;
        }

        protected virtual void PerformUpdate()
        {
            if (!enabled)
                return;
            if (m_PoseProviderComponent != null)
            {
                Pose providerPose;
                m_PoseProviderComponent.TryGetPoseFromProvider(out providerPose);
                SetLocalTransform(providerPose.position, providerPose.rotation);
            }
            else
            {
                SetLocalTransform(m_CurrentPosition, m_CurrentRotation);
            }
        }

    }
}
