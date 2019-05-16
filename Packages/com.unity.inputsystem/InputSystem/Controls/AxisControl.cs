using System;
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
    public class AxisControl : InputControl<float>
    {
        // These can be added as processors but they are so common that we
        // build the functionality right into AxisControl to save us an
        // additional object and an additional virtual call.
        public bool clamp; // If true, force clamping to [min..max]
        public bool clampToConstant; // If true, set value to clampConstant when incoming value is outside [min..max]
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

        protected float Preprocess(float value)
        {
            if (scale)
                value *= scaleFactor;
            if (clampToConstant)
            {
                if (value < clampMin || value > clampMax)
                    value = clampConstant;
            }
            else if (clamp)
                value = Mathf.Clamp(value, clampMin, clampMax);
            if (normalize)
                value = NormalizeProcessor.Normalize(value, normalizeMin, normalizeMax, normalizeZero);
            if (invert)
                value *= -1.0f;
            return value;
        }

        public AxisControl()
        {
            m_StateBlock.format = InputStateBlock.FormatFloat;
        }

        // Read a floating-point value from the given state. Automatically checks
        // the state format of the control and performs conversions.
        // NOTE: Throws if the format set on 'stateBlock' is not of integer, floating-point,
        //       or bitfield type.
        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            var value = stateBlock.ReadFloat(statePtr);
            ////REVIEW: this isn't very raw
            return Preprocess(value);
        }

        public override unsafe void WriteValueIntoState(float value, void* statePtr)
        {
            stateBlock.WriteFloat(statePtr, value);
        }

        public override unsafe bool CompareValue(void* firstStatePtr, void* secondStatePtr)
        {
            var currentValue = ReadValueFromState(firstStatePtr);
            var valueInState = ReadValueFromState(secondStatePtr);
            return !Mathf.Approximately(currentValue, valueInState);
        }

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
