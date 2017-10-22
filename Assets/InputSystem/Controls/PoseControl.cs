namespace ISX
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

        public override Pose value => Process(new Pose(position.value, rotation.value));
        public override Pose previous => Process(new Pose(position.previous, rotation.previous));

        protected override void FinishSetup(InputControlSetup setup)
        {
            position = setup.GetControl<Vector3Control>(this, "position");
            rotation = setup.GetControl<QuaternionControl>(this, "rotation");
            velocity = setup.GetControl<Vector3Control>(this, "velocity");
            angularVelocity = setup.GetControl<Vector3Control>(this, "angularVelocity");
            acceleration = setup.GetControl<Vector3Control>(this, "acceleration");
            angularAcceleration = setup.GetControl<Vector3Control>(this, "angularAcceleration");
            base.FinishSetup(setup);
        }
    }
}
