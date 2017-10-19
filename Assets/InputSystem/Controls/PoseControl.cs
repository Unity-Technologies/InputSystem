namespace ISX
{
    public class PoseControl : InputControl<Pose>
    {
        public Vector3Control translation { get; private set; }
        public QuaternionControl rotation { get; private set; }
        public Vector3Control velocity { get; private set; }
        public Vector3Control angularVelocity { get; private set; }
        public Vector3Control acceleration { get; private set; }
        public Vector3Control angularAcceleration { get; private set; }

        public PoseControl()
        {
            m_StateBlock.sizeInBits = sizeof(float) * (3 + 4) * 8;
            m_StateBlock.format = new FourCC('P', 'O', 'S', 'E');
        }

        public override Pose value => Process(new Pose(translation.value, rotation.value));
        public override Pose previous => Process(new Pose(translation.previous, rotation.previous));

        protected override void FinishSetup(InputControlSetup setup)
        {
            translation = setup.GetControl<Vector3Control>(this, "translation");
            rotation = setup.GetControl<QuaternionControl>(this, "rotation");
            velocity = setup.GetControl<Vector3Control>(this, "velocity");
            angularVelocity = setup.GetControl<Vector3Control>(this, "angularVelocity");
            acceleration = setup.GetControl<Vector3Control>(this, "acceleration");
            angularAcceleration = setup.GetControl<Vector3Control>(this, "angularAcceleration");
            base.FinishSetup(setup);
        }
    }
}
