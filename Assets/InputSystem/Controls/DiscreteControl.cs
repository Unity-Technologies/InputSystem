namespace ISX
{
    public class DiscreteControl : InputControl<int>
    {
        public DiscreteControl()
        {
            m_StateBlock.sizeInBits = sizeof(int) * 8;
        }

        public override int value
        {
            get
            {
                unsafe
                {
                    var value = *(int*)currentValuePtr;
                    return Process(value);
                }
            }
        }
    }
}
