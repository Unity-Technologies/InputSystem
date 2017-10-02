namespace ISX
{
    // Same as an axis but for output instead of input.
    public class MotorControl : AxisControl
    {
        public MotorControl()
        {
            m_StateBlock.usage = InputStateBlock.Usage.Output;
        }
    }
}