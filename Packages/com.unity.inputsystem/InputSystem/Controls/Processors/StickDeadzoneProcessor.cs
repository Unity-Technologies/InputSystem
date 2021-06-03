using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

////REVIEW: rename to RadialDeadzone

////TODO: add different deadzone shapes and/or option to min/max X and Y separately

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Processes a Vector2 to apply deadzoning according to the magnitude of the vector (rather
    /// than just clamping individual axes). Normalizes to the min/max range.
    /// </summary>
    /// <seealso cref="AxisDeadzoneProcessor"/>
    [Preserve]
    public class StickDeadzoneProcessor : InputProcessor<Vector2>
    {
        /// <summary>
        /// Value at which the lower bound deadzone starts.
        /// </summary>
        /// <remarks>
        /// Values in the input at or below min will get dropped and values
        /// will be scaled to the range between min and max.
        /// </remarks>
        public float min;
        public float max;

        private float minOrDefault => min == default ? InputSystem.settings.defaultDeadzoneMin : min;
        private float maxOrDefault => max == default ? InputSystem.settings.defaultDeadzoneMax : max;

        public override Vector2 Process(Vector2 value, InputControl control = null)
        {
            var magnitude = value.magnitude;
            var newMagnitude = GetDeadZoneAdjustedValue(magnitude);
            if (newMagnitude == 0)
                value = Vector2.zero;
            else
                value *= newMagnitude / magnitude;
            return value;
        }

        private float GetDeadZoneAdjustedValue(float value)
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

        public override string ToString()
        {
            return $"StickDeadzone(min={minOrDefault},max={maxOrDefault})";
        }
    }

    #if UNITY_EDITOR
    internal class StickDeadzoneProcessorEditor : InputParameterEditor<StickDeadzoneProcessor>
    {
        protected override void OnEnable()
        {
            m_MinSetting.Initialize("Min",
                "Vector length  below which input values will be clamped. After clamping, vector lengths will be renormalized to [0..1] between min and max.",
                "Default Deadzone Min",
                () => target.min, v => target.min = v,
                () => InputSystem.settings.defaultDeadzoneMin);
            m_MaxSetting.Initialize("Max",
                "Vector length above which input values will be clamped. After clamping, vector lengths will be renormalized to [0..1] between min and max.",
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
