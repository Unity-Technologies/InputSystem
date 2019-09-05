using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;

////REVIEW: change 'clampToConstant' to simply 'clampToMin'?

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A floating-point axis control.
    /// </summary>
    /// <remarks>
    /// Can optionally be configured to perform normalization.
    /// Stored as either a float, a short, a byte, or a single bit.
    /// </remarks>
    [Scripting.Preserve]
    public class AxisControl : InputControl<float>
    {
        /// <summary>
        /// Clamping behavior for an axis control.
        /// </summary>
        public enum Clamp
        {
            /// <summary>
            /// Do not clamp values.
            /// </summary>
            None = 0,

            /// <summary>
            /// Clamp values to <see cref="clampMin"/> and <see cref="clampMax"/>
            /// before normalizing the value.
            /// </summary>
            BeforeNormalize = 1,

            /// <summary>
            /// Clamp values to <see cref="clampMin"/> and <see cref="clampMax"/>
            /// after normalizing the value.
            /// </summary>
            AfterNormalize = 2,

            /// <summary>
            /// Clamp values any value below <see cref="clampMin"/> or above <see cref="clampMax"/>
            /// to <see cref="clampConstant"/> before normalizing the value.
            /// </summary>
            ToConstantBeforeNormalize = 3,
        }

        // These can be added as processors but they are so common that we
        // build the functionality right into AxisControl to save us an
        // additional object and an additional virtual call.

        /// <summary>
        /// Clamping behavior when reading values.
        /// </summary>
        /// <value>Clamping behavior.</value>
        /// <remarks>
        /// When a value is read from the control's state, it is first converted
        /// to a floating-point number.
        /// </remarks>
        public Clamp clamp;

        public float clampMin;
        public float clampMax;
        public float clampConstant;
        ////REVIEW: why not just roll this into scaleFactor?
        public bool invert; // If true, multiply by -1.
        public bool normalize;
        public float normalizeMin;
        public float normalizeMax;
        public float normalizeZero;
        ////REVIEW: why not just have a default scaleFactor of 1?
        public bool scale;
        public float scaleFactor;

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected float Preprocess(float value)
        {
            if (scale)
                value *= scaleFactor;
            if (clamp == Clamp.ToConstantBeforeNormalize)
            {
                if (value < clampMin || value > clampMax)
                    value = clampConstant;
            }
            else if (clamp == Clamp.BeforeNormalize)
                value = Mathf.Clamp(value, clampMin, clampMax);
            if (normalize)
                value = NormalizeProcessor.Normalize(value, normalizeMin, normalizeMax, normalizeZero);
            if (clamp == Clamp.AfterNormalize)
                value = Mathf.Clamp(value, clampMin, clampMax);
            if (invert)
                value *= -1.0f;
            return value;
        }

        /// <summary>
        /// Default-initialize the control.
        /// </summary>
        /// <remarks>
        /// Defaults the format to <see cref="InputStateBlock.FormatFloat"/>.
        /// </remarks>
        public AxisControl()
        {
            m_StateBlock.format = InputStateBlock.FormatFloat;
        }

        /// <inheritdoc />
        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            var value = stateBlock.ReadFloat(statePtr);
            ////REVIEW: this isn't very raw
            return Preprocess(value);
        }

        /// <inheritdoc />
        public override unsafe void WriteValueIntoState(float value, void* statePtr)
        {
            stateBlock.WriteFloat(statePtr, value);
        }

        /// <inheritdoc />
        public override unsafe bool CompareValue(void* firstStatePtr, void* secondStatePtr)
        {
            var currentValue = ReadValueFromState(firstStatePtr);
            var valueInState = ReadValueFromState(secondStatePtr);
            return !Mathf.Approximately(currentValue, valueInState);
        }

        /// <inheritdoc />
        public override unsafe float EvaluateMagnitude(void* statePtr)
        {
            if (m_MinValue.isEmpty || m_MaxValue.isEmpty)
                return -1;

            var value = ReadValueFromState(statePtr);
            var min = m_MinValue.ToSingle();
            var max = m_MaxValue.ToSingle();

            value = Mathf.Clamp(value, min, max);

            // If part of our range is in negative space, evaluate magnitude as two
            // separate subspaces.
            if (min < 0)
            {
                if (value < 0)
                    return NormalizeProcessor.Normalize(Mathf.Abs(value), 0, Mathf.Abs(min), 0);
                return NormalizeProcessor.Normalize(value, 0, max, 0);
            }

            return NormalizeProcessor.Normalize(value, min, max, 0);
        }
    }
}
