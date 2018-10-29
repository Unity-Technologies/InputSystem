using UnityEngine.Experimental.Input.LowLevel;

////REVIEW: introduce separate base class for ButtonControl and AxisControl instead of deriving ButtonControl from AxisControl?

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// An axis that has a trigger point beyond which it is considered to be pressed.
    /// </summary>
    /// <remarks>
    /// By default stored as a single bit. In that format, buttons will only yield 0
    /// and 1 as values.
    ///
    /// It may seem unnatural to derive ButtonControl from AxisControl, but
    /// doing so brings many benefits through allowing code to flexibly target buttons
    /// and axes the same way.
    /// </remarks>
    public class ButtonControl : AxisControl
    {
        public float pressPoint;
        public float pressPointOrDefault
        {
            get { return pressPoint > 0.0f ? pressPoint : InputConfiguration.ButtonPressPoint; }
        }

        public ButtonControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeBit;
        }

        protected bool IsValueConsideredPressed(float value)
        {
            return value >= pressPointOrDefault;
        }

        public bool isPressed
        {
            get { return IsValueConsideredPressed(ReadValue()); }
        }

        public bool wasJustPressed
        {
            get { return device.wasUpdatedThisFrame && IsValueConsideredPressed(ReadValue()) && !IsValueConsideredPressed(ReadPreviousValue()); }
        }

        public bool wasJustReleased
        {
            get { return device.wasUpdatedThisFrame && !IsValueConsideredPressed(ReadValue()) && IsValueConsideredPressed(ReadPreviousValue()); }
        }
    }
}
