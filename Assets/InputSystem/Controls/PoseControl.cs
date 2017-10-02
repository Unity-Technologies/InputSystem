namespace ISX
{
    public class PoseControl : InputControl<Pose>
    {
        public Vector3Control translation { get; private set; }
        public QuaternionControl rotation { get; private set; }

        public PoseControl()
        {
	        m_StateBlock.sizeInBits = sizeof(float)*(3+4)*8;
        }

        public override Pose value
        {
            get { return Process(new Pose(translation.value, rotation.value)); }
        }

	    protected override void FinishSetup(InputControlSetup setup)
	    {
		    translation = setup.GetControl<Vector3Control>(this, "translation");
		    rotation = setup.GetControl<QuaternionControl>(this, "rotation");
		    base.FinishSetup(setup);
	    }
    }
}