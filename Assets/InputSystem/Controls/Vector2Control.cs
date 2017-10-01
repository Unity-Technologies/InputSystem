using UnityEngine;

namespace InputSystem
{
	public class Vector2Control : InputControl<Vector2>
    {
        public AxisControl x { get; private set; }
        public AxisControl y { get; private set; }
	    
        public Vector2Control(string name)
            : base(name)
        {
        }
	    
        public override Vector2 value
        {
			get
			{
		        return Process(new Vector2(x.value, y.value));
			}
        }

	    protected override void FinishSetup(InputControlSetup setup)
	    {
		    x = setup.GetControl<AxisControl>(this, "x");
		    y = setup.GetControl<AxisControl>(this, "y");
		    base.FinishSetup(setup);
	    }
    }
}