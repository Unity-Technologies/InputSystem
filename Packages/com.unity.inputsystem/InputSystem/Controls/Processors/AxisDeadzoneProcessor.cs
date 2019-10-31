using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Processors
{
    [Preserve]
    internal class AxisDeadzoneProcessor : InputProcessor<float>
    {
        public float min;
        public float max;

        private float minOrDefault => min == default ? InputSystem.settings.defaultDeadzoneMin : min;
        private float maxOrDefault => max == default ? InputSystem.settings.defaultDeadzoneMax : max;

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
