using System;

namespace ISX
{
    ////TODO: allow format to be any integer format
    public class DiscreteControl : InputControl<int>
    {
        public DiscreteControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeInt;
        }

        private unsafe int GetValue(IntPtr valuePtr)
        {
            var value = *(int*)valuePtr;
            return Process(value);
        }

        public override int value => GetValue(currentValuePtr);
        public override int previous => GetValue(previousValuePtr);
    }
}
