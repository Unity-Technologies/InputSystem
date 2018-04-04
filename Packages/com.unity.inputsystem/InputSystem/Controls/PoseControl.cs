using System;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Controls
{
    public class PoseControl : InputControl<Pose>
    {
        [InputControl(bit = 0)]
        public ButtonControl positionAvailable { get; private set; }
        [InputControl(bit = 1)]
        public ButtonControl rotationAvailable { get; private set; }
        [InputControl(bit = 3)]
        public ButtonControl velocityAvailable { get; private set; }
        [InputControl(bit = 4)]
        public ButtonControl angularVelocityAvailable { get; private set; }
        [InputControl(bit = 5)]
        public ButtonControl accelerationAvailable { get; private set; }
        [InputControl(bit = 6)]
        public ButtonControl angularAccelerationAvailable { get; private set; }

        public Vector3Control position { get; private set; }
        public QuaternionControl rotation { get; private set; }
        public Vector3Control velocity { get; private set; }
        public Vector3Control angularVelocity { get; private set; }
        public Vector3Control acceleration { get; private set; }
        public Vector3Control angularAcceleration { get; private set; }

        public PoseControl()
        {
            m_StateBlock.format = new FourCC('P', 'O', 'S', 'E');
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            position = builder.GetControl<Vector3Control>(this, "position");
            rotation = builder.GetControl<QuaternionControl>(this, "rotation");
            velocity = builder.GetControl<Vector3Control>(this, "velocity");
            angularVelocity = builder.GetControl<Vector3Control>(this, "angularVelocity");
            acceleration = builder.GetControl<Vector3Control>(this, "acceleration");
            angularAcceleration = builder.GetControl<Vector3Control>(this, "angularAcceleration");
            base.FinishSetup(builder);
        }

        public override Pose ReadRawValueFrom(IntPtr statePtr)
        {
            return new Pose(position.ReadValueFrom(statePtr), rotation.ReadValueFrom(statePtr));
        }

        protected override void WriteRawValueInto(IntPtr statePtr, Pose value)
        {
            position.WriteValueInto(statePtr, value.translation);
            rotation.WriteValueInto(statePtr, value.rotation);
        }
    }
}
