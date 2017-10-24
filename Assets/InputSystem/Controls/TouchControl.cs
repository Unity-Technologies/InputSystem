namespace ISX
{
    public class TouchControl : InputControl<Touch>
    {
        [InputControl(alias = "pointerId", offset = 0)]
        public DiscreteControl touchId { get; private set; }
        [InputControl(usage = "Point", offset = 4)]
        public Vector2Control position { get; private set; }
        [InputControl(usage = "Secondary2DMotion", autoReset = true, offset = 12)]
        public Vector2Control delta { get; private set; }
        [InputControl(usage = "Pressure", offset = 20)]
        public AxisControl pressure { get; private set; }
        [InputControl(usage = "Radius", offset = 24)]
        public Vector2Control radius { get; private set; }
        [InputControl(format = "SHRT", offset = 32)]
        public DiscreteControl phase { get; private set; }
        [InputControl(format = "SHRT", offset = 34)]
        public DiscreteControl displayIndex { get; private set; }

        public TouchControl()
        {
            m_StateBlock.format = new FourCC('T', 'O', 'U', 'C');
        }

        //needs to enforce layout/format

        public override Touch value { get; }
        public override Touch previous { get; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            touchId = setup.GetControl<DiscreteControl>(this, "touchId");
            position = setup.GetControl<Vector2Control>(this, "position");
            delta = setup.GetControl<Vector2Control>(this, "delta");
            pressure = setup.GetControl<AxisControl>(this, "pressure");
            radius = setup.GetControl<Vector2Control>(this, "radius");
            phase = setup.GetControl<DiscreteControl>(this, "phase");
            displayIndex = setup.GetControl<DiscreteControl>(this, "displayIndex");
            base.FinishSetup(setup);
        }
    }
}
