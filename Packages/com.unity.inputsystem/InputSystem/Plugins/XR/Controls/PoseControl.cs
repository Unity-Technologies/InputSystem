using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;
using TrackingState = UnityEngine.XR.InputTrackingState;

namespace UnityEngine.InputSystem.XR
{
    [StructLayout(LayoutKind.Explicit, Size = kSizeInBytes)]
    public struct PoseState : IInputStateTypeInfo
    {
        internal const int kSizeInBytes = 60;

        public FourCC format => new FourCC('P', 'o', 's', 'e');

        public PoseState(bool isTracked, TrackingState trackingState, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            m_IsTracked = isTracked;
            m_TrackingState = trackingState;
            m_Position = position;
            m_Rotation = rotation;
            m_Velocity = velocity;
            m_AngularVelocity = angularVelocity;
        }

        [FieldOffset(0), InputControl(name = "isTracked", displayName = "Is Tracked", layout = "Button")]
        public bool m_IsTracked;

        [FieldOffset(4), InputControl(name = "trackingState", displayName = "Tracking State", layout = "Integer")]
        public TrackingState m_TrackingState;

        [FieldOffset(8), InputControl(name = "position", displayName = "Position", noisy = true)]
        public Vector3 m_Position;

        [FieldOffset(20), InputControl(name = "rotation", displayName = "Rotation", noisy = true)]
        public Quaternion m_Rotation;

        [FieldOffset(36), InputControl(name = "velocity", displayName = "Velocity", noisy = true)]
        public Vector3 m_Velocity;

        [FieldOffset(48), InputControl(name = "angularVelocity", displayName = "Angular Velocity", noisy = true)]
        public Vector3 m_AngularVelocity;

        public bool isTracked => m_IsTracked;
        public TrackingState trackingState => m_TrackingState;
        public Vector3 position => m_Position;
        public Quaternion rotation => m_Rotation;
        public Vector3 velocity => m_Velocity;
        public Vector3 angularVelocity => m_AngularVelocity;
    }

    [Preserve, InputControlLayout(stateType = typeof(PoseState))]
    public class PoseControl : InputControl<PoseState>
    {
        public ButtonControl isTracked { get; private set; }

        public IntegerControl trackingState { get; private set; }

        public Vector3Control position { get; private set; }

        public QuaternionControl rotation { get; private set; }

        public Vector3Control velocity { get; private set; }

        public Vector3Control angularVelocity { get; private set; }

        public PoseControl()
        {
            m_StateBlock.format = new FourCC('P', 'o', 's', 'e');
        }

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

        public override unsafe PoseState ReadUnprocessedValueFromState(void* statePtr)
        {
            var valuePtr = (PoseState*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
            return *valuePtr;
        }

        public override unsafe void WriteValueIntoState(PoseState value, void* statePtr)
        {
            var valuePtr = (PoseState*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
            UnsafeUtility.MemCpy(valuePtr, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<PoseState>());
        }
    }
}
