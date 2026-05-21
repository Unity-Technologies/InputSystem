#if UNITY_EDITOR
using System;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Processors
{
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
            if (!InputSystem.settings.useIMGUIEditorForAssets)
                return;

            m_MinSetting.OnGUI();
            m_MaxSetting.OnGUI();
        }

        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            m_MinSetting.OnDrawVisualElements(root, onChangedCallback);
            m_MaxSetting.OnDrawVisualElements(root, onChangedCallback);
            CustomOrDefaultSetting.AddSharedDefaultSettingsFooter(root,
                new[] { m_MinSetting, m_MaxSetting });
        }

        private CustomOrDefaultSetting m_MinSetting;
        private CustomOrDefaultSetting m_MaxSetting;
    }
}
#endif
