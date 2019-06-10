using UnityEngine.InputSystem.LowLevel;

////TODO: get rid of pressPoint and instead deadzone axis buttons

////REVIEW: introduce separate base class for ButtonControl and AxisControl instead of deriving ButtonControl from AxisControl?

namespace UnityEngine.InputSystem.Controls
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
        public float pressPointOrDefault => pressPoint > 0.0f ? pressPoint : InputSystem.settings.defaultButtonPressPoint;

        public ButtonControl()
        {
            m_StateBlock.format = InputStateBlock.FormatBit;
            m_MinValue = 0f;
            m_MaxValue = 1f;
        }

        public bool IsValueConsideredPressed(float value)
        {
            return value >= pressPointOrDefault;
        }

        public bool isPressed => IsValueConsideredPressed(ReadValue());

        public bool wasPressedThisFrame => device.wasUpdatedThisFrame && IsValueConsideredPressed(ReadValue()) && !IsValueConsideredPressed(ReadValueFromPreviousFrame());

        public bool wasReleasedThisFrame => device.wasUpdatedThisFrame && !IsValueConsideredPressed(ReadValue()) && IsValueConsideredPressed(ReadValueFromPreviousFrame());
    }
}
