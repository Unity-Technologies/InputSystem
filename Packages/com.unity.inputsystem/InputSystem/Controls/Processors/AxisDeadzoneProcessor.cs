using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Clamps values to the range given by <see cref="min"/> and <see cref="max"/> and re-normalizes the resulting
    /// value to [0..1].
    /// </summary>
    /// <remarks>
    /// This processor is registered (see <see cref="InputSystem.RegisterProcessor{T}"/>) under the name "AxisDeadzone".
    ///
    /// It acts like a combination of <see cref="ClampProcessor"/> and <see cref="NormalizeProcessor"/>.
    ///
    /// <example>
    /// <code>
    /// </code>
    /// // Bind to right trigger on gamepad such that the value is clamped and normalized between
    /// // 0.3 and 0.7.
    /// new InputAction(binding: "&lt;Gamepad&gt;/rightTrigger", processors: "axisDeadzone(min=0.3,max=0.7)");
    /// </example>
    /// </remarks>
    /// <seealso cref="StickDeadzoneProcessor"/>
    public class AxisDeadzoneProcessor : InputProcessor<float>
    {
        /// <summary>
        /// Lower bound (inclusive) below which input values get clamped. Corresponds to 0 in the normalized range.
        /// </summary>
        /// <remarks>
        /// If this is equal to 0 (the default), <see cref="InputSettings.defaultDeadzoneMin"/> is used instead.
        /// </remarks>
        public float min;

        /// <summary>
        /// Upper bound (inclusive) beyond which input values get clamped. Corresponds to 1 in the normalized range.
        /// </summary>
        /// <remarks>
        /// If this is equal to 0 (the default), <see cref="InputSettings.defaultDeadzoneMax"/> is used instead.
        /// </remarks>
        public float max;

        private float minOrDefault => min == default ? InputSystem.settings.defaultDeadzoneMin : min;
        private float maxOrDefault => max == default ? InputSystem.settings.defaultDeadzoneMax : max;

        /// <summary>
        /// Normalize <paramref name="value"/> according to <see cref="min"/> and <see cref="max"/>.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="control">Ignored.</param>
        /// <returns>Normalized value.</returns>
        public override float Process(float value, InputControl control = null)
        {
            var min = minOrDefault;
            var max = maxOrDefault;

            var absValue = Mathf.Abs(value);
            if (absValue < min)
                return 0;
            if (absValue > max)
                return Mathf.Sign(value);

            return Mathf.Sign(value) * ((absValue - min) / (max - min));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"AxisDeadzone(min={minOrDefault},max={maxOrDefault})";
        }
    }

    #if UNITY_EDITOR
    internal class AxisDeadzoneProcessorEditor : InputParameterEditor<AxisDeadzoneProcessor>
    {
        protected override void OnEnable()
        {
            m_MinSetting.Initialize("Min",
                "Value below which input values will be clamped. After clamping, values will be renormalized to [0..1] between min and max.",
                "Default Deadzone Min",
                () => target.min, v => target.min = v,
                () => InputSystem.settings.defaultDeadzoneMin);
            m_MaxSetting.Initialize("Max",
                "Value above which input values will be clamped. After clamping, values will be renormalized to [0..1] between min and max.",
                "Default Deadzone Max",
                () => target.max, v => target.max = v,
                () => InputSystem.settings.defaultDeadzoneMax);
        }

        public override void OnGUI()
        {
            m_MinSetting.OnGUI();
            m_MaxSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_MinSetting;
        private CustomOrDefaultSetting m_MaxSetting;
    }
    #endif
}
