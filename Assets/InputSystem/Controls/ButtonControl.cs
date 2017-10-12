namespace ISX
{
    // An axis that has a trigger point beyond which it is considered to be pressed.
    // By default stored as a single bit. In that format, buttons will only yield 0
    // and 1 as values.
    //
    // NOTE: While it may seem unnatural to derive ButtonControl from AxisControl, doing
    //       so brings many benefits through allowing code to flexibly target buttons
    //       and axes the same way.
    public class ButtonControl : AxisControl
    {
        public float pressPoint;

        public ButtonControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeBit;
        }

        protected bool IsValueConsideredPressed(float value)
        {
            var point = pressPoint;
            if (pressPoint <= 0.0f)
                point = InputConfiguration.ButtonPressPoint;
            return value >= point;
        }

        ////REVIEW: this may have to go into value itself; otherwise actions will trigger on the slightest value change
        public bool isPressed => IsValueConsideredPressed(value);
        public bool wasPressedThisFrame => IsValueConsideredPressed(value) && !IsValueConsideredPressed(previous);
        public bool wasReleasedThisFrame => !IsValueConsideredPressed(value) && IsValueConsideredPressed(previous);
    }
}
