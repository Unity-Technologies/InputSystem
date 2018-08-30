using System;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Processors;

////REVIEW: change 'clampToConstant' to simply 'clampToMin'?

namespace UnityEngine.Experimental.Input.Controls
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
        public bool invert; // If true, multiply by -1.
        public bool normalize;
        public float normalizeMin;
        public float normalizeMax;
        public float normalizeZero;

        protected float Preprocess(float value)
        {
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
            m_StateBlock.format = InputStateBlock.kTypeFloat;
        }

        public override bool HasSignificantChange(InputEventPtr eventPtr)
        {
            float value;
            if (ReadValueFrom(eventPtr, out value))
                return Mathf.Abs(value - ReadDefaultValue()) > float.Epsilon;
            return false;
        }

        // Read a floating-point value from the given state. Automatically checks
        // the state format of the control and performs conversions.
        // NOTE: Throws if the format set on 'stateBlock' is not of integer, floating-point,
        //       or bitfield type.
        public override float ReadUnprocessedValueFrom(IntPtr statePtr)
        {
            var value = stateBlock.ReadFloat(statePtr);
            ////REVIEW: this isn't very raw
            return Preprocess(value);
        }

        protected override void WriteUnprocessedValueInto(IntPtr statePtr, float value)
        {
            stateBlock.WriteFloat(statePtr, value);
        }
    }
}
