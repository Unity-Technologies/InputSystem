namespace ISX
{
    public class PointerControl : Vector2Control
    {
        public DiscreteControl id { get; private set; }
        public DiscreteControl phase { get; private set; }

        [InputControl(usage = "Pressure")]
        public AxisControl pressure { get; private set; }

        public Vector2Control delta { get; private set; }
    }
}
