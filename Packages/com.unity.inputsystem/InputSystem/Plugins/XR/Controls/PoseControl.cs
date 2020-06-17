#if UNITY_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;
using TrackingState = UnityEngine.XR.InputTrackingState;

namespace UnityEngine.InputSystem.XR
{
    public struct Pose
    {
        public bool isTracked { get; set; }
        public TrackingState trackingState { get; set; }

        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }

        public Vector3 velocity { get; set; }
        public Vector3 angularVelocity { get; set; }
    }

    public class PoseControl : InputControl<Pose>
    {
        [Preserve]
        [InputControl(offset = 0)]
        public ButtonControl isTracked { get; private set; }

        [Preserve]
        [InputControl(offset = 4)]
        public IntegerControl trackingState { get; private set; }

        [Preserve]
        [InputControl(offset = 8, noisy = true)]
        public Vector3Control position { get; private set; }

        [Preserve]
        [InputControl(offset = 20, noisy = true)]
        public QuaternionControl rotation { get; private set; }

        [Preserve]
        [InputControl(offset = 36, noisy = true)]
        public Vector3Control velocity { get; private set; }

        [Preserve]
        [InputControl(offset = 48, noisy = true)]
        public Vector3Control angularVelocity { get; private set; }

        public PoseControl()
        { }

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

        public override unsafe Pose ReadUnprocessedValueFromState(void* statePtr)
        {
            return new Pose()
            {
                isTracked = isTracked.ReadUnprocessedValueFromState(statePtr) > 0.5f,
                trackingState = (TrackingState)trackingState.ReadUnprocessedValueFromState(statePtr),
                position = position.ReadUnprocessedValueFromState(statePtr),
                rotation = rotation.ReadUnprocessedValueFromState(statePtr),
                velocity = velocity.ReadUnprocessedValueFromState(statePtr),
                angularVelocity = angularVelocity.ReadUnprocessedValueFromState(statePtr),
            };
        }

        public override unsafe void WriteValueIntoState(Pose value, void* statePtr)
        {
            isTracked.WriteValueIntoState(value.isTracked, statePtr);
            trackingState.WriteValueIntoState((uint)value.trackingState, statePtr);
            position.WriteValueIntoState(value.position, statePtr);
            rotation.WriteValueIntoState(value.rotation, statePtr);
            velocity.WriteValueIntoState(value.velocity, statePtr);
            angularVelocity.WriteValueIntoState(value.angularVelocity, statePtr);
        }
    }
}
#endif
