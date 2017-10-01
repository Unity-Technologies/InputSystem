namespace InputSystem
{
    // Same as an axis but for output instead of input.
    public class MotorControl : AxisControl
    {
        public MotorControl(string name)
            : base(name)
        {
            stateBlock.usage = InputStateBlock.Usage.Output;
        }
    }
}