#if UNITY_EDITOR
using System;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Interactions
{
    internal class SlowTapInteractionEditor : InputParameterEditor<SlowTapInteraction>
    {
        protected override void OnEnable()
        {
            m_DurationSetting.Initialize("Min Tap Duration",
                "Minimum time (in seconds) that a control has to be held for it to register as a slow tap. If the control is released "
                + "before this time, the slow tap is canceled.",
                "Default Slow Tap Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultSlowTapTime);
            m_PressPointSetting.Initialize("Press Point",
                "The amount of actuation a control requires before being considered pressed. If not set, default to "
                + "'Default Button Press Point' in the global input settings.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v,
                () => InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
            if (!InputSystem.settings.useIMGUIEditorForAssets)
                return;

            m_DurationSetting.OnGUI();
            m_PressPointSetting.OnGUI();
        }

        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            m_DurationSetting.OnDrawVisualElements(root, onChangedCallback);
            m_PressPointSetting.OnDrawVisualElements(root, onChangedCallback);
            CustomOrDefaultSetting.AddSharedDefaultSettingsFooter(root,
                new[] { m_DurationSetting, m_PressPointSetting });
        }

        private CustomOrDefaultSetting m_DurationSetting;
        private CustomOrDefaultSetting m_PressPointSetting;
    }
}
#endif
