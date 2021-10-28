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

        // NOTE: A number of the parameters here can be expressed in much simpler form.
        //       E.g. 'scale', 'scaleFactor' and 'invert' could all be rolled into a single
        //       multiplier. And maybe that's what we should do. However, the one advantage
        //       of the current setup is that it allows to set these operations up individually.
        //       For example, a given layout may want to have a very specific scale factor but
        //       then a derived layout needs the value to be inverted. If it was a single setting,
        //       the derived layout would have to know the specific scale factor in order to come
        //       up with a valid multiplier.

        /// <summary>
        /// Clamping behavior when reading values. <see cref="Clamp.None"/> by default.
        /// </summary>
        /// <value>Clamping behavior.</value>
        /// <remarks>
        /// When a value is read from the control's state, it is first converted
        /// to a floating-point number.
        /// </remarks>
        /// <seealso cref="clampMin"/>
        /// <seealso cref="clampMax"/>
        /// <seealso cref="clampConstant"/>
        public Clamp clamp;

        /// <summary>
        /// Lower end of the clamping range when <see cref="clamp"/> is not
        /// <see cref="Clamp.None"/>.
        /// </summary>
        /// <value>Lower bound of clamping range. Inclusive.</value>
        public float clampMin;

        /// <summary>
        /// Upper end of the clamping range when <see cref="clamp"/> is not
        /// <see cref="Clamp.None"/>.
        /// </summary>
        /// <value>Upper bound of clamping range. Inclusive.</value>
        public float clampMax;

        /// <summary>
        /// When <see cref="clamp"/> is set to <see cref="Clamp.ToConstantBeforeNormalize"/>
        /// and the value is outside of the range defined by <see cref="clampMin"/> and
        /// <see cref="clampMax"/>, this value is returned.
        /// </summary>
        /// <value>Constant value to return when value is outside of clamping range.</value>
        public float clampConstant;

        ////REVIEW: why not just roll this into scaleFactor?
        /// <summary>
        /// If true, the input value will be inverted, i.e. multiplied by -1. Off by default.
        /// </summary>
        /// <value>Whether to invert the input value.</value>
        public bool invert;

        /// <summary>
        /// If true, normalize the input value to [0..1] or [-1..1] (depending on the
        /// value of <see cref="normalizeZero"/>. Off by default.
        /// </summary>
        /// <value>Whether to normalize input values or not.</value>
        /// <seealso cref="normalizeMin"/>
        /// <seealso cref="normalizeMax"/>
        public bool normalize;

        ////REVIEW: shouldn't these come from the control min/max value by default?

        /// <summary>
        /// If <see cref="normalize"/> is on, this is the input value that corresponds
        /// to 0 of the normalized [0..1] or [-1..1] range.
        /// </summary>
        /// <value>Input value that should become 0 or -1.</value>
        /// <remarks>
        /// In other words, with <see cref="normalize"/> on, input values are mapped from
        /// the range of [normalizeMin..normalizeMax] to [0..1] or [-1..1] (depending on
        /// <see cref="normalizeZero"/>).
        /// </remarks>
        public float normalizeMin;

        /// <summary>
        /// If <see cref="normalize"/> is on, this is the input value that corresponds
        /// to 1 of the normalized [0..1] or [-1..1] range.
        /// </summary>
        /// <value>Input value that should become 1.</value>
        /// <remarks>
        /// In other words, with <see cref="normalize"/> on, input values are mapped from
        /// the range of [normalizeMin..normalizeMax] to [0..1] or [-1..1] (depending on
        /// <see cref="normalizeZero"/>).
        /// </remarks>
        public float normalizeMax;

        /// <summary>
        /// Where to put the zero point of the normalization range. Only relevant
        /// if <see cref="normalize"/> is set to true. Defaults to 0.
        /// </summary>
        /// <value>Zero point of normalization range.</value>
        /// <remarks>
        /// The value of this property determines where the zero point is located in the
        /// range established by <see cref="normalizeMin"/> and <see cref="normalizeMax"/>.
        ///
        /// If <c>normalizeZero</c> is placed at <see cref="normalizeMin"/>, the normalization
        /// returns a value in the [0..1] range mapped from the input value range of
        /// <see cref="normalizeMin"/> and <see cref="normalizeMax"/>.
        ///
        /// If <c>normalizeZero</c> is placed in-between <see cref="normalizeMin"/> and
        /// <see cref="normalizeMax"/>, normalization returns a value in the [-1..1] mapped
        /// from the input value range of <see cref="normalizeMin"/> and <see cref="normalizeMax"/>
        /// and the zero point between the two established by <c>normalizeZero</c>.
        /// </remarks>
        public float normalizeZero;

        ////REVIEW: why not just have a default scaleFactor of 1?

        /// <summary>
        /// Whether the scale the input value by <see cref="scaleFactor"/>. Off by default.
        /// </summary>
        /// <value>True if inputs should be scaled by <see cref="scaleFactor"/>.</value>
        public bool scale;

        /// <summary>
        /// Value to multiple input values with. Only applied if <see cref="scale"/> is <c>true</c>.
        /// </summary>
        /// <value>Multiplier for input values.</value>
        public float scaleFactor;

        /// <summary>
        /// Apply modifications to the given value according to the parameters configured
        /// on the control (<see cref="clamp"/>, <see cref="normalize"/>, etc).
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <returns>A processed value (clamped, normalized, etc).</returns>
        /// <seealso cref="clamp"/>
        /// <seealso cref="normalize"/>
        /// <seealso cref="scale"/>
        /// <seealso cref="invert"/>
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

        private float Unpreprocess(float value)
        {
            // Does not reverse the effect of clamping (we don't know what the unclamped value should be).

            if (invert)
                value *= -1f;
            if (normalize)
                value = NormalizeProcessor.Denormalize(value, normalizeMin, normalizeMax, normalizeZero);
            if (scale)
                value /= scaleFactor;
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

        protected override void FinishSetup()
        {
            base.FinishSetup();

            // if we don't have any default state, and we are using normalizeZero, then the default value
            // should not be zero. Generate it from normalizeZero.
            if (!hasDefaultState && normalize && Mathf.Abs(normalizeZero) > Mathf.Epsilon)
                m_DefaultState = stateBlock.FloatToPrimitiveValue(normalizeZero);
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
            value = Unpreprocess(value);
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
            var value = ReadValueFromState(statePtr);
            if (m_MinValue.isEmpty || m_MaxValue.isEmpty)
                return Mathf.Abs(value);

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
