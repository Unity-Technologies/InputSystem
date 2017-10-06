using System;

namespace ISX
{
    public class DiscreteControl : InputControl<int>
    {
        public DiscreteControl()
        {
            m_StateBlock.sizeInBits = sizeof(int) * 8;
        }

        private unsafe int GetValue(IntPtr valuePtr)
        {
            var value = *(int*)currentValuePtr;
            return Process(value);
        }

        public override int value => GetValue(currentValuePtr);
        public override int previous => GetValue(previousValuePtr);
    }
}
