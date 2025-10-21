// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF || PACKAGE_DOCS_GENERATION
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;
using TrackingState = UnityEngine.XR.InputTrackingState;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// State layout for a single pose.
    /// </summary>
    /// <remarks>
    /// This is the low-level memory representation of a single pose, i.e the
    /// way poses are internally transmitted and stored in the system. PoseStates are used on devices containing <see cref="PoseControl"/>s.
    /// </remarks>
    /// <seealso cref="PoseControl"/>
    [StructLayout(LayoutKind.Explicit, Size = kSizeInBytes)]
    public struct PoseState : IInputStateTypeInfo
    {
        internal const int kSizeInBytes = 60;

        internal static readonly FourCC s_Format = new FourCC('P', 'o', 's', 'e');

        /// <summary>
        /// Memory format tag for PoseState.
        /// </summary>
        /// <value>Returns "Pose".</value>
        /// <seealso cref="InputStateBlock.format"/>
        public FourCC format => s_Format;

        /// <summary>
        /// Constructor for PoseStates.
        ///
        /// Useful for creating PoseStates locally (not from <see cref="PoseControl"/>).
        /// </summary>
        /// <param name="isTracked">Value to use for <see cref="isTracked"/></param>
        /// <param name="trackingState">Value to use for <see cref="trackingState"/></param>
        /// <param name="position">Value to use for <see cref="position"/></param>
        /// <param name="rotation">Value to use for <see cref="rotation"/></param>
        /// <param name="velocity">Value to use for <see cref="velocity"/></param>
        /// <param name="angularVelocity">Value to use for <see cref="angularVelocity"/></param>
        public PoseState(bool isTracked, TrackingState trackingState, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            this.isTracked = isTracked;
            this.trackingState = trackingState;
            this.position = position;
            this.rotation = rotation;
            this.velocity = velocity;
            this.angularVelocity = angularVelocity;
        }

        /// <summary>
        /// Whether the pose is currently being fully tracked. Otherwise, the tracking is either unavailable, or simulated.
        /// </summary>
        /// <remarks>
        /// Fully tracked means that the pose is accurate and not using any simulated or extrapolated positions, and the system tracking this pose is able to confidently track this object.
        /// </remarks>
        [FieldOffset(0), InputControl(displayName = "Is Tracked", layout = "Button", sizeInBits = 8 /* needed to ensure optimization kicks-in */)]
        public bool isTracked;

        /// <summary>
        /// A Flags Enumeration specifying which other fields in the pose state are valid.
        /// </summary>
        [FieldOffset(4), InputControl(displayName = "Tracking State", layout = "Integer")]
        public TrackingState trackingState;

        /// <summary>
        /// The position in 3D space, relative to the tracking origin where this pose represents.
        /// </summary>
        /// <remarks>
        /// Positions are represented in meters.
        /// This field is only valid if <see cref="trackingState"/> contains the <see cref="UnityEngine.XR.InputTrackingState.Position"/> value.
        /// See <seealso cref="UnityEngine.XR.TrackingOriginModeFlags"/> for information on tracking origins.
        /// </remarks>
        [FieldOffset(8), InputControl(displayName = "Position", noisy = true)]
        public Vector3 position;

        /// <summary>
        /// The rotation in 3D space, relative to the tracking origin where this pose represents.
        /// </summary>
        /// <remarks>
        /// This field is only valid if <see cref="trackingState"/> contains the <see cref="UnityEngine.XR.InputTrackingState.Rotation"/> value.
        /// See <seealso cref="UnityEngine.XR.TrackingOriginModeFlags"/> for information on tracking origins.
        /// </remarks>
        [FieldOffset(20), InputControl(displayName = "Rotation", noisy = true)]
        public Quaternion rotation;

        /// <summary>
        /// The velocity in 3D space, relative to the tracking origin where this pose represents.
        /// </summary>
        /// <remarks>
        /// Velocities are represented in meters per second.
        /// This field is only valid if <see cref="trackingState"/> contains the <see cref="UnityEngine.XR.InputTrackingState.Velocity"/> value.
        /// See <seealso cref="UnityEngine.XR.TrackingOriginModeFlags"/> for information on tracking origins.
        /// </remarks>
        [FieldOffset(36), InputControl(displayName = "Velocity", noisy = true)]
        public Vector3 velocity;

        /// <summary>
        /// The angular velocity in 3D space, relative to the tracking origin where this pose represents.
        /// </summary>
        /// <remarks>
        /// This field is only valid if <see cref="trackingState"/> contains the <see cref="UnityEngine.XR.InputTrackingState.AngularVelocity"/> value.
        /// See <seealso cref="UnityEngine.XR.TrackingOriginModeFlags"/> for information on tracking origins.
        /// </remarks>
        [FieldOffset(48), InputControl(displayName = "Angular Velocity", noisy = true)]
        public Vector3 angularVelocity;
    }

    /// <summary>
    /// A control representing a Pose in 3D space, relative to an XR tracking origin
    /// </summary>
    /// <remarks>
    /// Note that unlike most other control types, <c>PoseControls</c> do not have
    /// a flexible memory layout. They are hardwired to <see cref="PoseState"/> and
    /// will not work correctly with a different memory layouts. Additional fields may
    /// be appended to the struct but what's there in the struct has to be located
    /// at exactly those memory addresses.
    ///
    /// For more information on tracking origins see <see cref="UnityEngine.XR.TrackingOriginModeFlags"/>.
    /// </remarks>
    [Preserve, InputControlLayout(stateType = typeof(PoseState))]
    public class PoseControl : InputControl<PoseState>
    {
        /// <summary>
        /// Represents whether this pose is fully tracked or unavailable/simulated.
        /// </summary>
        /// <value>Control representing whether the pose is being fully tracked. Maps to the <see cref="PoseState.isTracked"/> value.</value>
        /// <seealso cref="PoseState.isTracked"/>
        public ButtonControl isTracked { get; set; }

        /// <summary>
        /// The other controls on this <see cref="PoseControl"/> that are currently reporting data.
        /// </summary>
        /// <remarks>
        /// This can be missing values when the device tracking this pose is restricted or not tracking properly.
        /// </remarks>
        /// <value>Control representing whether the pose is being fully tracked. Maps to the <see cref="PoseState.trackingState"/> value of the pose retrieved from this control.</value>
        /// <seealso cref="PoseState.trackingState"/>
        public IntegerControl trackingState { get; set; }

        /// <summary>
        /// The position, in meters, of this tracked pose relative to the tracking origin.
        /// </summary>
        /// <remarks>
        /// The data for this control is only valid if the value returned from <see cref="trackingState"/> contains <see cref="UnityEngine.XR.InputTrackingState.Position"/> value.
        /// </remarks>
        /// <value>Control representing whether the pose is being fully tracked. Maps to the <see cref="PoseState.position"/> value of the pose retrieved from this control.</value>
        /// <seealso cref="PoseState.position"/>
        public Vector3Control position { get; set; }

        /// <summary>
        /// The rotation of this tracked pose relative to the tracking origin.
        /// </summary>
        /// <remarks>
        /// The data for this control is only valid if the value returned from <see cref="trackingState"/> contains <see cref="UnityEngine.XR.InputTrackingState.Rotation"/> value.
        /// </remarks>
        /// <value>Control representing whether the pose is being fully tracked. Maps to the <see cref="PoseState.rotation"/> value of the pose retrieved from this control.</value>
        /// <seealso cref="PoseState.rotation"/>
        public QuaternionControl rotation { get; set; }

        /// <summary>
        /// The velocity, in meters per second, of this tracked pose relative to the tracking origin.
        /// </summary>
        /// <remarks>
        /// The data for this control is only valid if the value returned from <see cref="trackingState"/> contains <see cref="UnityEngine.XR.InputTrackingState.Velocity"/> value.
        /// </remarks>
        /// <value>Control representing whether the pose is being fully tracked. Maps to the <see cref="PoseState.velocity"/> value of the pose retrieved from this control.</value>
        /// <seealso cref="PoseState.velocity"/>
        public Vector3Control velocity { get; set; }

        /// <summary>
        /// The angular velocity of this tracked pose relative to the tracking origin.
        /// </summary>
        /// <remarks>
        /// The data for this control is only valid if the value returned from <see cref="trackingState"/> contains <see cref="UnityEngine.XR.InputTrackingState.AngularVelocity"/> value.
        /// </remarks>
        /// <value>Control representing whether the pose is being fully tracked. Maps to the <see cref="PoseState.angularVelocity"/> value of the pose retrieved from this control.</value>
        /// <seealso cref="PoseState.angularVelocity"/>
        public Vector3Control angularVelocity { get; set; }

        /// <summary>
        /// Default-initialize the pose control.
        /// </summary>
        /// <remarks>
        /// Sets the <see cref="InputStateBlock.format"/> to <c>"Pose"</c>.
        /// </remarks>
        public PoseControl()
        {
            m_StateBlock.format = PoseState.s_Format;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            isTracked = GetChildControl<ButtonControl>("isTracked");
            trackingState = GetChildControl<IntegerControl>("trackingState");
            position = GetChildControl<Vector3Control>("position");
            rotation = GetChildControl<QuaternionControl>("rotation");
            velocity = GetChildControl<Vector3Control>("velocity");
            angularVelocity = GetChildControl<Vector3Control>("angularVelocity");

            base.FinishSetup();
        }

        /// <inheritdoc />
        public override unsafe PoseState ReadUnprocessedValueFromState(void* statePtr)
        {
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatPose:
                    return *(PoseState*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
                default:
                    return new PoseState()
                    {
                        isTracked = isTracked.ReadUnprocessedValueFromStateWithCaching(statePtr) > 0.5f,
                        trackingState = (TrackingState)trackingState.ReadUnprocessedValueFromStateWithCaching(statePtr),
                        position = position.ReadUnprocessedValueFromStateWithCaching(statePtr),
                        rotation = rotation.ReadUnprocessedValueFromStateWithCaching(statePtr),
                        velocity = velocity.ReadUnprocessedValueFromStateWithCaching(statePtr),
                        angularVelocity = angularVelocity.ReadUnprocessedValueFromStateWithCaching(statePtr),
                    };
            }
        }

        /// <inheritdoc />
        public override unsafe void WriteValueIntoState(PoseState value, void* statePtr)
        {
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatPose:
                    *(PoseState*)((byte*)statePtr + (int)m_StateBlock.byteOffset) = value;
                    break;
                default:
                    isTracked.WriteValueIntoState(value.isTracked, statePtr);
                    trackingState.WriteValueIntoState((uint)value.trackingState, statePtr);
                    position.WriteValueIntoState(value.position, statePtr);
                    rotation.WriteValueIntoState(value.rotation, statePtr);
                    velocity.WriteValueIntoState(value.velocity, statePtr);
                    angularVelocity.WriteValueIntoState(value.angularVelocity, statePtr);
                    break;
            }
        }

        protected override FourCC CalculateOptimizedControlDataType()
        {
            if (
                m_StateBlock.sizeInBits == PoseState.kSizeInBytes * 8 &&
                m_StateBlock.bitOffset == 0 &&
                isTracked.optimizedControlDataType == InputStateBlock.kFormatByte &&
                trackingState.optimizedControlDataType == InputStateBlock.kFormatInt &&
                position.optimizedControlDataType == InputStateBlock.kFormatVector3 &&
                rotation.optimizedControlDataType == InputStateBlock.kFormatQuaternion &&
                velocity.optimizedControlDataType == InputStateBlock.kFormatVector3 &&
                angularVelocity.optimizedControlDataType == InputStateBlock.kFormatVector3 &&
                trackingState.m_StateBlock.byteOffset == isTracked.m_StateBlock.byteOffset + 4 &&
                position.m_StateBlock.byteOffset == isTracked.m_StateBlock.byteOffset + 8 &&
                rotation.m_StateBlock.byteOffset == isTracked.m_StateBlock.byteOffset + 20 &&
                velocity.m_StateBlock.byteOffset == isTracked.m_StateBlock.byteOffset + 36 &&
                angularVelocity.m_StateBlock.byteOffset == isTracked.m_StateBlock.byteOffset + 48
            )
                return InputStateBlock.kFormatPose;

            return InputStateBlock.kFormatInvalid;
        }
    }
}
#endif
