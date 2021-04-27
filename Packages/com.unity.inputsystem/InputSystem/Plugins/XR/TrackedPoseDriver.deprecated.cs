using System;

namespace UnityEngine.InputSystem.XR
{
    public partial class TrackedPoseDriver : ISerializationCallbackReceiver
    {
        // Disable warnings that these fields are never assigned to. They are set during Unity deserialization and migrated.
        // ReSharper disable UnassignedField.Local
#pragma warning disable 0649
        [SerializeField, HideInInspector]
        InputAction m_PositionAction;
        [Obsolete("positionAction has been deprecated. Use positionInput instead.")]
        public InputAction positionAction
        {
            get => m_PositionInput.action;
            set => positionInput = new InputActionProperty(value);
        }

        [SerializeField, HideInInspector]
        InputAction m_RotationAction;
        [Obsolete("rotationAction has been deprecated. Use rotationInput instead.")]
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

            m_PositionInput = new InputActionProperty(m_PositionAction);
            m_RotationInput = new InputActionProperty(m_RotationAction);
            m_HasMigratedActions = true;
        }
    }
}
