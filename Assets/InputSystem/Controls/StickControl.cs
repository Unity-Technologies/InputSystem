namespace ISX
{
    // A two-axis thumbstick control that can act as both a vector and a four-way dpad.
    // State-wise this is still just a Vector2.
    public class StickControl : Vector2Control
    {
	    [InputControl(options="clamp=true,clampMin=0,clampMax=1")]
        public AxisControl up { get; private set; }
	    [InputControl(options="clamp=true,clampMin=-1,clampMax=0")]
        public AxisControl down { get; private set; }
	    [InputControl(options="clamp=true,clampMin=-1,clampMap=0")]
        public AxisControl left { get; private set; }
	    [InputControl(options="clamp=true,clampMin=0,clampMax=1")]
        public AxisControl right { get; private set; }

	    protected override void FinishSetup(InputControlSetup setup)
	    {
		    up = setup.GetControl<AxisControl>(this, "up");
		    down = setup.GetControl<AxisControl>(this, "down");
		    left = setup.GetControl<AxisControl>(this, "left");
		    right = setup.GetControl<AxisControl>(this, "right");
	        
		    base.FinishSetup(setup);
	    }
    }
}